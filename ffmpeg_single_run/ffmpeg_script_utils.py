from datetime import datetime as dt
from glob import glob
import os
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

#### DICTIONARIES/STATICS ####
video_root_dir = '/nas'
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
	def __init__(self):
		self.hdr = None
		self.crop = ""
		self.codec = ""
		self.orig_resolution = ""
		self.color_space = ""
		self.color_primaries = ""
		self.color_transfer = ""

class AudioData:
	def __init__(self):
		self.index = -1
		self.descriptor = ""
		self.language = ""
		self.channels = -1
		self.channel_layout = ""

class SubtitleData:
	def __init__(self):
		self.language = ""
		self.descriptor = ""
		self.index = -1

class EncodeData:
	def __init__(self):
		self.source_file_full_path = ""
		self.video_data = VideoData()
		self.audio_data = []
		self.subtitle_data = None

#### CLASSES ####

#### FUNCTIONS ####
# Print out usage of script
def usage():
	print('Usage: python3 %s [-m movie | -t tv_show_episode | -a anime_movie]' % sys.argv[0])

# Creates a sorted video list based on the given video type and user input
# Returns: Sorted list of video files
def create_video_list(video_type, user_input):
	search_dir = dir_lookup[video_type]

	full_search = '%s/%s/*%s*' % (video_root_dir, search_dir, user_input)

	video_file_list = glob(full_search)
	additional_videos = []

	for entry in video_file_list:
		if os.path.isdir('%s' % entry):
			video_list = glob('%s/*%s*' % (entry, user_input))
			
			for video in video_list:
				additional_videos.append(video)

			video_file_list.remove(entry)

	for video in additional_videos:
		video_file_list.append(video)

	video_file_list.sort()

	return video_file_list

# Provides a list of movies for the user to select from.
# Returns: The full path for the selected video
def user_select_video_file(video_list):
	for i in range(0, len(video_list)):
		print("%s) %s" % (i + 1, os.path.basename(video_list[i])))

	option = input("Select video file: ")
	try:
		option_int = int(option)
	except:
		print("INVALID INPUT")
		sys.exit(2)

	if (1 <= option_int <= len(video_list)):
		return video_list[option_int - 1]
	else:
		print("INVALID OPTION SELECTED")
		sys.exit(2)

# Runs a subprocess of ffprobe to create an xml file with data on the given video file
# Returns: the name of the xml file which is based on the name of the video file
def create_video_data_xml(video_full_path):
	print('### Running ffprobe to generate xml file with video details to analyze.')
	xml_filename = '%s.xml' % os.path.basename(video_full_path)
	proc = subprocess.run('ffprobe -v quiet -read_intervals "%+#2" -print_format xml -show_format -show_streams -show_entries side_data "{}" > "{}"'.format(video_full_path, xml_filename), shell=True)
	return xml_filename

# Analyzes ffprobe xml output of file and builds a EncodeData object with details needed to run ffmpeg.
# Takes in the xml_root object (not the file object)
# Returns: EncodeData object
def build_encode_data(selected_video_full_path, xml_root):
	encode_data = EncodeData()
	encode_data.source_file_full_path = selected_video_full_path

	# HDR
	print('### Getting HDR Data')
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
	print('### Getting Video, Audio, and Subtitles Data')
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
				print('Unknown audio codec type found. Add to dictionary. Codec Type: %s' % codec_name)
				sys.exit(1)

			tags = stream.findall('tag')
			language = None
			for tag in tags:
				key = tag.get('key')
				if key == 'language':
					language = tag.get('value')
					break

			# Exit if for some reason no language is found.
			if language == None:
				print('Unknown language for stream index %s. Exiting.' % index)
				sys.exit(1)

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
			print('Unable to identify stream/codec type for stream %s.  Exiting.' % index)
			sys.exit(1)

	# Crop
	print('### Getting Video Crop Data (Warning: Can take a little bit of time.)')
	crop = subprocess.check_output("""ffmpeg -i "%s" -ss 00:10:00 -t 00:00:30 -vf cropdetect -f null - 2>&1 | awk '/crop/ { print $NF }' | tail -1""" % selected_video_full_path, shell=True, encoding='UTF-8')
	encode_data.video_data.crop = crop.rstrip('\n\r')

	return encode_data

# Prints info the video file/encode.
# Requires user to confirm to continue. Deletes created xml file since no longer needed.
def confirm_encode(encode_data, xml_filename):
	print('### Confirm movie encode details\n')
	print('\t%s' % os.path.basename(encode_data.source_file_full_path))
	print('\tVideo: %s (%s) %s' % (encode_data.video_data.codec, encode_data.video_data.orig_resolution, 'No HDR' if encode_data.video_data.hdr == None else 'HDR'))
	print('\tAudio:')
	for audio in encode_data.audio_data:
		print('\t\t%s (%s) | %s => aac (stereo)' % (audio.descriptor, audio.language, audio.channel_layout))
		if (audio.descriptor != 'ac3'):
			print('\t\t%s (%s) | %s => copy' % (audio.descriptor, audio.language, audio.channel_layout))

	if encode_data.subtitle_data != None:
		print('\tSubtitles: %s (%s)\n' % (encode_data.subtitle_data.language, encode_data.subtitle_data.descriptor))
	else:
		print('\tSubtitles: None\n')

	confirm = input('Confirm (y/n): ')

	if (confirm == 'y') or (confirm == 'Y'):
		print('### Deleting xml file (%s).' % xml_filename)
		os.remove(xml_filename)
	else:
		print('Encode data not confirmed (check script/video file) or invalid input. Deleting xml file and exiting.')
		os.remove(xml_filename)
		sys.exit(1)

# Creates ffmpeg command
# Returns: ffmpeg command string
def build_encode_command(encode_data):
	print('### Building ffmpeg encode command')
	audio_list = encode_data.audio_data
	video_data = encode_data.video_data
	subtitle_data = encode_data.subtitle_data

	# Map section
	map_str = '-map 0:v:0 '
	for audio in audio_list:
		if audio.descriptor == 'ac3':
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
		video_settings_str = '-pix_fmt yuv420p -vcodec libx264 -vf "%s" -preset veryslow -crf 16 ' % video_data.crop

	# Audio section
	audio_settings_str = ''
	audio_count = 0
	for audio in audio_list:
		if audio.descriptor == 'ac3':
			audio_settings_str += '-c:a:%d aac -ac 2 -filter:a:0 "aresample=matrix_encoding=dplii" -metadata:s:a:%d title="Stereo (%s)" ' % (audio_count, audio_count, audio.language)
			audio_count += 1

		else:
			audio_settings_str += '-c:a:%d aac -ac 2 -filter:a:0 "aresample=matrix_encoding=dplii" -metadata:s:a:%d title="Stereo (%s)" -c:a:%d copy ' % (audio_count, audio_count, audio.language, audio_count + 1)
			audio_count += 2
	
	# Subtitle section
	subtitle_settings_str = ''
	if subtitle_data != None:
		subtitle_settings_str = '-c:s copy '

	cmd = 'ffmpeg -y -i "%s" %s%s%s%s-max_muxing_queue_size 9999 "/nas/Movies (Encoded)/%s"' \
		% (encode_data.source_file_full_path, map_str, video_settings_str, audio_settings_str, subtitle_settings_str, os.path.basename(encode_data.source_file_full_path))

	return cmd


#### FUNCTIONS ####