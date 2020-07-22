from glob import glob
import os
from pathlib import Path
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

#### DICTIONARIES/STATICS ####
# The min value to use x265 for video encoding
# Multiply the width and height of the video and compare to this value
# This value is based on 720p
min_265_res_val = 921600

dir_lookup = {
		'-m' : 'Movies',
		'--movie' : 'Movies',
		'-t' : 'TV Shows',
		'--tv' : 'TV Shows',
		'-a' : 'Anime',
		'--anime' : 'Anime',
		'-am' : 'Anime Movie',
		'--anime_movie' : 'Anime Movies',
	}

audio_codec_priority = {
		'truehd' : 10,
		'DTS-HD MA' : 8,
		'dts' : 5,
		'DTS' : 5,
		'pcm_s24le' : 4,
		'pcm_s16le' : 4,
		'ac3' : 3
}

#### DICTIONARIES/STATICS ####

#### CLASSES ####
class HDRData:
	__slots__ = ['red_x', 'red_y', 'green_x', 'green_y', 'blue_x', 'blue_y', 'white_point_x', 'white_point_y', 'min_luminance', 'max_luminance']
	def __init__(self):
		self.red_x = ""
		self.red_y = ""
		self.green_x = ""
		self.green_y = ""
		self.blue_x = ""
		self.blue_y = ""
		self.white_point_x = ""
		self.white_point_y = ""
		self.min_luminance = ""
		self.max_luminance = ""

class VideoData:
	__slots__ = ['hdr', 'crop', 'codec', 'orig_resolution', 'color_space', 'color_primaries', 'color_transfer']
	def __init__(self):
		self.hdr = None
		self.crop = ""
		self.codec = ""
		self.orig_resolution = ""
		self.color_space = ""
		self.color_primaries = ""
		self.color_transfer = ""

class AudioData:
	__slots__ = ['index', 'descriptor', 'language', 'channels', 'channel_layout']
	def __init__(self):
		self.index = -1
		self.descriptor = ""
		self.language = ""
		self.channels = -1
		self.channel_layout = ""

class SubtitleData:
	__slots__ = ['language', 'descriptor', 'index']
	def __init__(self):
		self.language = ""
		self.descriptor = ""
		self.index = -1

class EncodeData:
	__slots__ = ['source_file_full_path', 'video_data', 'audio_data', 'subtitle_data']
	def __init__(self):
		self.source_file_full_path = ""
		self.video_data = VideoData()
		self.audio_data = []
		self.subtitle_data = None

#### CLASSES ####

#### FUNCTIONS ####
# Builds encode data log message
# Returns: Log Message
def __build_encode_data_log_msg(encode_data):
	msg = ['Encode Data: %s' % os.path.basename(encode_data.source_file_full_path)]

	video_str = 'Video: %s %s%s %s' % (
		encode_data.video_data.orig_resolution, 
		'' if encode_data.video_data.hdr == None else '(HDR) ', 
		encode_data.video_data.crop, 
		encode_data.video_data.codec)
	msg.append(video_str)

	for audio in encode_data.audio_data:
		audio_str = 'Audio: %s %s (%s) => AAC (Stereo)' % (audio.descriptor, audio.channel_layout, audio.language)
		msg.append(audio_str)
		if ((audio.descriptor != 'ac3') or (audio.channels > 2)):
			audio_str = 'Audio: %s %s (%s) => Copy' % (audio.descriptor, audio.channel_layout, audio.language)
			msg.append(audio_str)

	if encode_data.subtitle_data != None:
		subtitle_str = 'Subtitle: %s (%s)' % (encode_data.subtitle_data.language, encode_data.subtitle_data.descriptor)
	else:
		subtitle_str = 'Subtitle: NONE'
	msg.append(subtitle_str)

	return msg

# Builds a list of movie files and their base filename based on the given directory
# Mainly used to build lists of source movies to be used in determining what movies need 
# to be encoded
# Returns: Tuple with index 0 being movie files, and index 1 being base filenames
def build_movie_lists(movie_dir):
	movie_files = []
	movie_files_base = []
	for file in glob('%s/**/*.mkv' % movie_dir, recursive=True):
		movie_files.append(file)
		movie_files_base.append(Path(file).stem)

	movie_files.sort()
	movie_files_base.sort()

	return (movie_files, movie_files_base)

# Builds a list of movie files and their base filename based on the given directory.
# Used for directory containing already encoded files (which could have different file extensions).
# The list of extensions to use is based on if movies are being looked at for re-encoding or being encoded
# for the first time.
# Returns: Tuple with index 0 being movie files, and index 1 being base filenames
def build_movie_encoded_lists(movie_encoded_dir):
	movie_encoded_files = []
	movie_encoded_files_base = []

	file_extensions = ('*.mkv', '*.m4v', '*.mp4')

	for file in file_extensions:
		movie_encoded_files.extend(glob('%s/**/%s' % (movie_encoded_dir, file), recursive=True))

	movie_encoded_files.sort()

	for m in movie_encoded_files:
		movie_encoded_files_base.append(Path(m).stem)

	return (movie_encoded_files, movie_encoded_files_base)

# Builds a list of new movie files to encode
# Returns: List of new movie files to encode
def build_to_encode_list(movie_files, movie_files_base, movie_encoded_files_base):
	# List of differences in base file lists
	diff_list = [m for m in movie_files_base if m not in movie_encoded_files_base]

	# Uses diff_list base file names to find full file path in movie_files
	to_encode = [m for m in movie_files if any(sub in m for sub in diff_list)]

	return to_encode

# Checks the state of the file to see if it is ready to encode
# Currently checks to see if the file size is changing to see if it is being
# written to
# Returns: Tuple(Bool (True if ready to encode, False otherwise), message)
def check_file_ready(video_full_path):
	is_ready = False
	file_size_before = Path(video_full_path).stat().st_size
	time.sleep(5)
	file_size_after = Path(video_full_path).stat().st_size

	if file_size_before != file_size_after:
		msg = 'File %s is being written to (file size is changing). May still be ripping. Aborting encoding.' % video_full_path
	else:
		is_ready = True
		msg = 'File %s is ready.' % video_full_path

	return (is_ready, msg)

# Runs a subprocess of ffprobe to create an xml file with data on the given video file
# Returns: Tuple(the path of the xml file which is based on the name of the video file (None if error), msg)
def create_video_data_xml(video_full_path):
	xml_file_path = '/tmp/%s.xml' % os.path.basename(video_full_path)
	proc = subprocess.run('ffprobe -v quiet -read_intervals "%+#2" -print_format xml -show_format -show_streams -show_entries side_data "{}" > "{}"'.format(video_full_path, xml_file_path), shell=True, stderr=subprocess.PIPE)

	if proc.returncode != 0:
		error_msg = proc.stderr.decode('utf-8'.split('\n'))
		msg = 'Error running ffprobe for %s. Details below' % movie
		error_msg.insert(0, msg)
		return (None, error_msg)
	else:
		return (xml_file_path, 'Created xml file to be analyzed for %s (XML File Location: %s)' % (video_full_path, xml_file_path))

# Analyzes ffprobe xml output of file and builds a EncodeData object with details needed to run ffmpeg.
# Takes in the xml_root object (not the file object)
# Returns: Tuple(EncodeData object (or None if error), message)
def build_encode_data(movie_full_path, xml_file_path):
	xml_file = ET.parse(xml_file_path)
	xml_root = xml_file.getroot()

	encode_data = EncodeData()
	encode_data.source_file_full_path = movie_full_path

	# HDR
	for frame in xml_root.findall('packets_and_frames/frame'):
		side_data_list = frame.find('side_data_list') #.find('side_data').get('side_data_type')
		if side_data_list != None:
			side_data = side_data_list.find('side_data')
			if side_data != None:
				side_data_type = side_data.get('side_data_type')
				if side_data_type == 'Mastering display metadata':
					encode_data.video_data.hdr = HDRData()
					encode_data.video_data.hdr.red_x = side_data.get('red_x').split('/')[0]
					encode_data.video_data.hdr.red_y = side_data.get('red_y').split('/')[0]
					encode_data.video_data.hdr.green_x = side_data.get('green_x').split('/')[0]
					encode_data.video_data.hdr.green_y = side_data.get('green_y').split('/')[0]
					encode_data.video_data.hdr.blue_x = side_data.get('blue_x').split('/')[0]
					encode_data.video_data.hdr.blue_y = side_data.get('blue_y').split('/')[0]
					encode_data.video_data.hdr.white_point_x = side_data.get('white_point_x').split('/')[0]
					encode_data.video_data.hdr.white_point_y = side_data.get('white_point_y').split('/')[0]
					encode_data.video_data.hdr.min_luminance = side_data.get('min_luminance').split('/')[0]
					encode_data.video_data.hdr.max_luminance = side_data.get('max_luminance').split('/')[0]
					break
				else:
					continue
			else:
				continue
		else:
			continue

	# Streams (Video, Audio, Subtitles)
	for stream in xml_root.findall('streams/stream'):
		subtitle_index = 0 #Used to keep track of index to use in ffmpeg command for subtitles (in case more than one subtitle stream is found)

		codec_type = stream.get('codec_type')
		if codec_type == 'video':
			width = int(stream.get('width'))
			height = int(stream.get('height'))
			resolution_val = width * height
			if resolution_val >= min_265_res_val:
				encode_data.video_data.codec = 'libx265'

				color_space = stream.get('color_space')
				encode_data.video_data.color_space = color_space if color_space != None else 'bt709'

				color_transfer = stream.get('color_transfer')
				encode_data.video_data.color_transfer = color_transfer if color_transfer != None else 'bt709'

				color_primaries = stream.get('color_primaries')
				encode_data.video_data.color_primaries = color_primaries if color_primaries != None else 'bt709'
			else:
				encode_data.video_data.codec = 'libx264'
			encode_data.video_data.orig_resolution = '%dx%d' % (width, height)
		elif codec_type == 'audio':
			index = int(stream.get('index'))
			codec_name = stream.get('codec_name')
			if codec_name == 'dts':
				codec_name = stream.get('profile')
			codec_priority = audio_codec_priority.get(codec_name)

			# Unknown codec; Won't know what to do with it and should be added to dictionary.
			if codec_priority == None:
				msg = 'Unknown audio codec type found. Add to dictionary. Codec Type: %s' % codec_name
				return (None, msg)

			tags = stream.findall('tag')
			language = None
			for tag in tags:
				key = tag.get('key')
				if key == 'language':
					language = tag.get('value')
					break

			# Exit if for some reason no language is found.
			if language == None:
				msg = 'Unknown language for stream index %s.' % index
				return (None, msg)

			# Check if we have ANY audio streams
			if not encode_data.audio_data:
				audio_data = AudioData()
				audio_data.index = 0 #First audio stream found
				audio_data.descriptor = codec_name
				audio_data.language = language
				audio_data.channels = int(stream.get('channels'))
				audio_data.channel_layout = stream.get('channel_layout')
				encode_data.audio_data.append(audio_data)
			else:
				found = False
				for i in range(0, len(encode_data.audio_data)):
					if encode_data.audio_data[i].language == language:
						found = True
						current_priority = audio_codec_priority[encode_data.audio_data[i].descriptor]
						if codec_priority > current_priority:
							audio_data = AudioData()
							audio_data.index = index - 1
							audio_data.descriptor = codec_name
							audio_data.language = language
							audio_data.channels = int(stream.get('channels'))
							audio_data.channel_layout = stream.get('channel_layout')
							encode_data.audio_data[i] = audio_data
						else:
							break

				# Didn't find audio data with a matching language, add a new language
				if found == False:
					audio_data = AudioData()
					audio_data.index = index - 1
					audio_data.descriptor = codec_name
					audio_data.language = language
					audio_data.channels = int(stream.get('channels'))
					audio_data.channel_layout = stream.get('channel_layout')
					encode_data.audio_data.append(audio_data)

		elif codec_type == 'subtitle':
			# Only need 1 (english) subtitle; TODO: Forced subtitle tracks?
			if encode_data.subtitle_data == None:
				tags = stream.findall('tag')
				for tag in tags:
					key = tag.get('key')
					if key == 'language':
						language = tag.get('value')
						if language != 'eng':
							continue
						else:
							encode_data.subtitle_data = SubtitleData()
							break

				# If not english language, subtitle data won't be made
				if encode_data.subtitle_data != None:
					encode_data.subtitle_data.language = language
					encode_data.subtitle_data.descriptor = stream.get('codec_name')
					encode_data.subtitle_data.index = subtitle_index

			subtitle_index += 1

		else:
			index = stream.get('index')
			msg = 'Unable to identify stream/codec type for stream %s.' % index
			return (None, msg)

	# Crop
	crop = subprocess.check_output("""ffmpeg -i "%s" -ss 00:10:00 -t 00:00:30 -vf cropdetect -f null - 2>&1 | awk '/crop/ { print $NF }' | tail -1""" % movie_full_path, shell=True, encoding='UTF-8')
	encode_data.video_data.crop = crop.rstrip('\n\r')

	msg = ['Built encode data for %s' % movie_full_path] + __build_encode_data_log_msg(encode_data)

	return (encode_data, msg)

# Creates ffmpeg command
# Returns: Tuple(ffmpeg command string, encoded_file_destination_path)
def build_encode_command(encode_data, dest_dir):
	dest_path = '%s/%s' % (dest_dir, os.path.basename(encode_data.source_file_full_path))
	audio_list = encode_data.audio_data
	video_data = encode_data.video_data
	subtitle_data = encode_data.subtitle_data

	# Map section
	map_str = '-map 0:v:0 '
	for audio in audio_list:
		if audio.descriptor == 'ac3':
			if audio.channels > 2:
				map_str += '-map 0:a:%s -map 0:a:%s ' % (audio.index, audio.index)
			else:
				map_str += '-map 0:a:%s ' % (audio.index)
		else:
			map_str += '-map 0:a:%s -map 0:a:%s ' % (audio.index, audio.index)
	if subtitle_data != None:
		map_str += '-map 0:s:%s ' % subtitle_data.index

	# Video section
	if video_data.codec == 'libx265':
		video_settings_str = '-pix_fmt yuv420p10le -vcodec libx265 -vf "%s" -preset slow ' % video_data.crop
		if video_data.hdr != None:
			hdr = video_data.hdr
			master_display_str = "master-display='G(%s,%s)B(%s,%s)R(%s,%s)WP(%s,%s)L(%s,%s)'" % (hdr.green_x, video_data.hdr.green_y, hdr.blue_x, hdr.blue_y, hdr.red_x, hdr.red_y, hdr.white_point_x, hdr.white_point_y, hdr.max_luminance, hdr.min_luminance)
			video_settings_str += '-x265-params "keyint=60:bframes=3:vbv-bufsize=75000:vbv-maxrate=75000:colorprim=%s:transfer=%s:colormatrix=%s:%s" -crf 22 -force_key_frames "expr:gte(t,n_forced*2)" ' \
				% (video_data.color_primaries, video_data.color_transfer, video_data.color_space, master_display_str)

		else:
			video_settings_str += '-x265-params "keyint=60:bframes=3:vbv-bufsize=75000:vbv-maxrate=75000:colorprim=%s:transfer=%s:colormatrix=%s" -crf 22 -force_key_frames "expr:gte(t,n_forced*2)" ' \
				% (video_data.color_primaries, video_data.color_transfer, video_data.color_space)	

	elif video_data.codec == 'libx264':
		video_settings_str = '-pix_fmt yuv420p -vcodec libx264 -vf "%s" -preset slow -crf 16 ' % video_data.crop

	# Audio section
	audio_settings_str = ''
	audio_count = 0
	for audio in audio_list:
		if audio.descriptor == 'ac3':
			if audio.channels > 2:
				audio_settings_str += '-c:a:%d libfdk_aac -ac 2 -filter:a:0 "aresample=matrix_encoding=dplii" -metadata:s:a:%d title="Stereo (%s)" -c:a:%d copy ' % (audio_count, audio_count, audio.language, audio_count + 1)
				audio_count += 2
			else:
				audio_settings_str += '-c:a:%d libfdk_aac -ac 2 -filter:a:0 "aresample=matrix_encoding=dplii" -metadata:s:a:%d title="Stereo (%s)" ' % (audio_count, audio_count, audio.language)
				audio_count += 1

		else:
			audio_settings_str += '-c:a:%d libfdk_aac -ac 2 -filter:a:0 "aresample=matrix_encoding=dplii" -metadata:s:a:%d title="Stereo (%s)" -c:a:%d copy ' % (audio_count, audio_count, audio.language, audio_count + 1)
			audio_count += 2
	
	# Subtitle section
	subtitle_settings_str = ''
	if subtitle_data != None:
		subtitle_settings_str = '-c:s copy '

	# TEMP CHANGE FOR TESTING
	cmd = 'ffmpeg -y -i "%s" %s%s%s%s-max_muxing_queue_size 9999 "%s"' \
		% (encode_data.source_file_full_path, map_str, video_settings_str, audio_settings_str, subtitle_settings_str, dest_path)

	return (cmd, dest_path)

#### FUNCTIONS ####