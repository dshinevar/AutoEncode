from enum import Enum, IntEnum
import itertools
import os
from subprocess import Popen
import subprocess
import traceback
import xml.etree.ElementTree as ET

from ffmpeg_tools_utilities import convert_seconds_to_timestamp

#### DICTIONARIES/STATICS ####
# The min value to use x265 for video encoding
# Multiply the width and height of the video and compare to this value
# This value is based on 720p
MIN_X265_RES_VALUE = 921600

# Priority of 1 or less are "undesirable".
# Unless the audio channels is greater than 2,
# any codec with priority 1 or less will be encoded to aac
audio_codec_priority = {
		'truehd' : 10,
		'DTS-HD MA' : 9,
		'pcm_s24le' : 8,
		'pcm_s16le' : 7,
		'DTS-HD HRA' : 6,
		'DTS-ES' : 5,
		'dts' : 4,
		'DTS' : 4,
		'ac3' : 1
}

#### DICTIONARIES/STATICS ####

#### CLASSES ####
class VideoEncoder(Enum):
	LIBX264 = 1
	LIBX265 = 2

class AudioEncodeProcess(Enum):
	COPY = 1
	COPY_WITH_AAC_STEREO = 2
	AAC_STEREO = 3

class VideoScan(IntEnum):
	INTERLACED_TFF = 0
	INTERLACED_BFF = 1
	PROGRESSIVE = 2

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
	__slots__ = ['hdr', 'crop', 'encoder', 'orig_resolution', 'color_space', 'color_primaries', 'color_transfer', 'max_cll', 'chroma_location', 'animated', 'scan']
	def __init__(self):
		self.hdr = None
		self.crop = ""
		self.encoder = None
		self.orig_resolution = ""
		self.color_space = ""
		self.color_primaries = ""
		self.color_transfer = ""
		self.max_cll = None
		self.chroma_location = None
		self.animated = False
		self.scan = None

class AudioData:
	__slots__ = ['index', 'stream_index', 'descriptor', 'language', 'channels', 'channel_layout', 'priority', 'commentary', 'encode_process']
	def __init__(self):
		self.index = -1
		self.stream_index = -1
		self.descriptor = ""
		self.language = ""
		self.channels = -1
		self.channel_layout = ""
		self.priority = -1
		self.commentary = False
		self.encode_process = AudioEncodeProcess.COPY_WITH_AAC_STEREO

class SubtitleData:
	__slots__ = ['index', 'stream_index', 'language', 'descriptor', 'forced']
	def __init__(self):
		self.index = -1
		self.stream_index = -1
		self.language = ""
		self.descriptor = ""
		self.forced = False

class EncodeData:
	__slots__ = ['source_file_full_path', 'video_data', 'audio_data', 'subtitle_data', 'subtitle_forced_data']
	def __init__(self):
		self.source_file_full_path = ""
		self.video_data = VideoData()
		self.audio_data = []
		self.subtitle_data = None
		self.subtitle_forced_data = None

class StreamData:
	__slots__ = ['source_file_full_path', 'video_stream', 'audio_streams', 'subtitle_streams']
	def __init__(self):
		self.source_file_full_path = ""
		self.video_stream = VideoData()
		self.audio_streams = []
		self.subtitle_streams = []

#### CLASSES ####

### FUNCTIONS ####
# Builds encode data log message
# Returns: Log Message
def __build_encode_data_log_msg(encode_data):
	msg = [f'Name: {os.path.basename(encode_data.source_file_full_path)}']

	video_encoder = ''
	if encode_data.video_data.encoder == VideoEncoder.LIBX265:
		video_encoder = 'libx265'
	elif encode_data.video_data.encoder == VideoEncoder.LIBX264:
		video_encoder = 'libx264'
	hdr = '' if encode_data.video_data.hdr == None else '(HDR) '
	crop = '' if encode_data.video_data.crop == None else encode_data.video_data.crop
	scan = ''
	if encode_data.video_data.scan == VideoScan.PROGRESSIVE:
		scan = 'Progressive'
	elif encode_data.video_data.scan == VideoScan.INTERLACED_TFF:
		scan = '(Interlaced TFF => Progressive)'
	elif encode_data.video_data.scan == VideoScan.INTERLACED_TFF:
		scan = '(Interlaced BFF => Progressive)'
	video_str = f'Video: {encode_data.video_data.orig_resolution} {scan}{hdr}{crop} {video_encoder}'
	msg.append(video_str)

	for audio in encode_data.audio_data:
		if audio.encode_process == AudioEncodeProcess.COPY:
			if audio.commentary == True:
				audio_str = f'Audio (Commentary): {audio.descriptor} {audio.channel_layout} ({audio.language}) => Copy'
			else:
				audio_str = f'Audio: {audio.descriptor} {audio.channel_layout} ({audio.language}) => Copy'
			msg.append(audio_str)
		elif audio.encode_process == AudioEncodeProcess.COPY_WITH_AAC_STEREO:
			audio_str = f'Audio: {audio.descriptor} {audio.channel_layout} ({audio.language}) => Copy'
			msg.append(audio_str)
			audio_str = f'Audio: {audio.descriptor} {audio.channel_layout} ({audio.language}) => AAC (Stereo)'
			msg.append(audio_str)
		elif audio.encode_process == AudioEncodeProcess.AAC_STEREO:
			audio_str = f'Audio: {audio.descriptor} {audio.channel_layout} ({audio.language}) => AAC (Stereo)'
			msg.append(audio_str)

	if (encode_data.subtitle_data == None) and (encode_data.subtitle_forced_data == None):
		subtitle_str = 'Subtitle: NONE'
		msg.append(subtitle_str)
	else:
		if encode_data.subtitle_data != None:
			subtitle_str = f'Subtitle: {encode_data.subtitle_data.language} ({encode_data.subtitle_data.descriptor})'
			msg.append(subtitle_str)
		if encode_data.subtitle_forced_data != None:
			subtitle_str = f'Subtitle (forced): {encode_data.subtitle_forced_data.language} ({encode_data.subtitle_forced_data.descriptor})'
			msg.append(subtitle_str)

	return msg

# Gets HDR data from xml file (xml_root)
# Returns: Tuple (HDRData, max_cll)
def __get_xml_hdr(xml_root):
	hdr = None
	max_cll = None
	for frame in xml_root.findall('packets_and_frames/frame'):
		side_data_list = frame.find('side_data_list') #.find('side_data').get('side_data_type')
		if side_data_list != None:
			side_data = side_data_list.findall('side_data')
			for data in side_data:
				side_data_type = data.get('side_data_type')
				if side_data_type == 'Mastering display metadata':
					hdr = HDRData()
					hdr.red_x = data.get('red_x').split('/')[0]
					hdr.red_y = data.get('red_y').split('/')[0]
					hdr.green_x = data.get('green_x').split('/')[0]
					hdr.green_y = data.get('green_y').split('/')[0]
					hdr.blue_x = data.get('blue_x').split('/')[0]
					hdr.blue_y = data.get('blue_y').split('/')[0]
					hdr.white_point_x = data.get('white_point_x').split('/')[0]
					hdr.white_point_y = data.get('white_point_y').split('/')[0]
					hdr.min_luminance = data.get('min_luminance').split('/')[0]
					hdr.max_luminance = data.get('max_luminance').split('/')[0]
				elif side_data_type == 'Content light level metadata':
					max_content = data.get('max_content')
					if max_content == None:
						max_content = '0'

					max_avg = data.get('max_average')
					if max_avg == None:
						max_avg = '0'

					max_cll = f"'{max_content},{max_avg}'"
				else:
					continue
			else:
				continue
		else:
			continue

	return (hdr, max_cll)

def __get_crop(video_full_path, start_timestamp):
	crop = subprocess.check_output("""ffmpeg -i "%s" -ss %s -t 00:02:00 -vf cropdetect -f null - 2>&1 | awk '/crop/ { print $NF }' | tail -1""" % (video_full_path, start_timestamp), shell=True, encoding='UTF-8')
	return crop.strip('\r\n')

def __get_scan(video_full_path):
	scan_raw = subprocess.check_output("""ffmpeg -filter:v idet -frames:v 10000 -an -f rawvideo -y /dev/null -i "%s" 2>&1 | awk '/frame detection/ {print $8, $10, $12}'""" % (video_full_path), shell=True, encoding='UTF-8')
	scan = list(filter(None, scan_raw.split('\n')))
	frame_totals = [0, 0, 0]

	for frames in scan:
		counts = frames.split(' ')
		# Should always be the order of: TFF, BFF, PROG
		frame_totals[VideoScan.INTERLACED_TFF] += int(counts[0])
		frame_totals[VideoScan.INTERLACED_BFF] += int(counts[1])
		frame_totals[VideoScan.PROGRESSIVE] += int(counts[2])

	video_scan = VideoScan(frame_totals.index(max(frame_totals)))
	return video_scan

# Runs a subprocess of ffprobe to create an xml file with data on the given video file
# Assumes /tmp/automated_ffmpeg/ exists and is writable
# Returns: Tuple(the path of the xml file which is based on the name of the video file (None if error), msg)
def create_video_data_xml(video_full_path):
	xml_file_path = f'/tmp/automated_ffmpeg/{os.path.basename(video_full_path)}.xml'
	proc = subprocess.run('ffprobe -v quiet -read_intervals "%+#2" -print_format xml -show_format -show_streams -show_entries side_data "{}" > "{}"'.format(video_full_path, xml_file_path), shell=True, stderr=subprocess.PIPE)

	if proc.returncode != 0:
		error_msg = proc.stderr.decode('utf-8').split('\n')
		msg = f'Error running ffprobe for {video_full_path}. Details below.'
		error_msg.insert(0, msg)
		return (None, error_msg)
	else:
		return (xml_file_path, f'Created xml file to be analyzed for {os.path.basename(video_full_path)} (XML File Location: {xml_file_path})')

# Compiles data from all streams in given xml_file
# Returns: StreamData object
def build_stream_data(xml_file_path, video_full_path):
	xml_file = ET.parse(xml_file_path)
	xml_root = xml_file.getroot()

	stream_data = StreamData()
	stream_data.source_file_full_path = video_full_path

	hdr, max_cll = __get_xml_hdr(xml_root)

	if hdr != None:
		stream_data.video_stream.hdr = hdr

	if max_cll != None:
		stream_data.video_stream.max_cll = max_cll

	# Streams (Video, Audio, Subtitles)
	audio_index = 0
	subtitle_index = 0 # Used to keep track of index to use in ffmpeg command for subtitles (in case more than one subtitle stream is found)
	for stream in xml_root.findall('streams/stream'):
		codec_type = stream.get('codec_type')
		if codec_type == 'video':
			width = int(stream.get('width'))
			height = int(stream.get('height'))
			stream_data.video_stream.orig_resolution = f'{width}x{height}'
			resolution_val = width * height
			if resolution_val >= MIN_X265_RES_VALUE:
				stream_data.video_stream.encoder = VideoEncoder.LIBX265

				color_space = stream.get('color_space')
				stream_data.video_stream.color_space = color_space if color_space != None else 'bt709'

				color_transfer = stream.get('color_transfer')
				stream_data.video_stream.color_transfer = color_transfer if color_transfer != None else 'bt709'

				color_primaries = stream.get('color_primaries')
				stream_data.video_stream.color_primaries = color_primaries if color_primaries != None else 'bt709'

				chroma_location = stream.get('chroma_location')
				if chroma_location != None:
					if chroma_location == 'topleft':
						stream_data.video_stream.chroma_location = '2'
					elif chroma_location == 'left':
						stream_data.video_stream.chroma_location = '1'
					else: # Default?
						stream_data.video_stream.chroma_location = '1'
			else:
				stream_data.video_stream.encoder = VideoEncoder.LIBX264

		elif codec_type == 'audio':
			audio_stream = AudioData()
			audio_stream.stream_index = int(stream.get('index'))
			audio_stream.index = audio_index
			codec_name = stream.get('codec_name')
			if codec_name == 'dts':
				profile = stream.get('profile')
				codec_name = f'{codec_name}/{profile}'

			audio_stream.descriptor = codec_name
			audio_stream.channels = int(stream.get('channels'))

			tags = stream.findall('tag')
			language = None
			title = None
			for tag in tags:
				key = tag.get('key')
				if key == 'language':
					language = tag.get('value')
				elif key == 'title':
					title = tag.get('value')
				elif (title != None) and (language != None):
					break

			if language == None:
				language = 'Unknown'
			audio_stream.language = language

			if (title != None) and ("Commentary" in title):
				audio_stream.commentary = True

			channel_layout = stream.get('channel_layout')
			if channel_layout != None:
				audio_stream.channel_layout = channel_layout
			elif title != None:
				audio_stream.channel_layout = title
			else:
				audio_stream.channel_layout = f'{audio_stream.channels}-channel(s)'

			stream_data.audio_streams.append(audio_stream)
			audio_index += 1
		elif codec_type == 'subtitle':
			subtitle_stream = SubtitleData()
			subtitle_stream.stream_index = int(stream.get('index'))
			subtitle_stream.index = subtitle_index
			subtitle_stream.descriptor = stream.get('codec_name')

			language = None
			tags = stream.findall('tag')
			for tag in tags:
				key = tag.get('key')
				if key == 'language':
					language = tag.get('value')

			subtitle_stream.language = language

			disposition = stream.find('disposition')
			default = disposition.get('default')
			forced = disposition.get('forced')

			if (default == '1') or (forced == '1'):
				subtitle_stream.forced = True

			stream_data.subtitle_streams.append(subtitle_stream)
			subtitle_index += 1

	format_section = xml_root.find('format')
	duration_seconds = int(float(format_section.get('duration')))
	duration_half = int(duration_seconds / 2)
	timestamp_half = convert_seconds_to_timestamp(duration_half)

	# Crop, Scan
	#crop = subprocess.check_output("""ffmpeg -i "%s" -ss %s -t 00:02:00 -vf cropdetect -f null - 2>&1 | awk '/crop/ { print $NF }' | tail -1""" % (video_full_path, timestamp_half), shell=True, encoding='UTF-8')
	#stream_data.video_stream.crop = crop.strip('\r\n')
	stream_data.video_stream.crop = __get_crop(video_full_path, timestamp_half)
	stream_data.video_stream.scan = __get_scan(video_full_path)

	return stream_data

# Analyzes ffprobe xml output of file and builds a EncodeData object with details needed to run ffmpeg.
# Takes in the xml_root object (not the file object)
# Returns: Tuple(EncodeData object (or None if error), message)
def build_automated_encode_data(xml_file_path, movie_full_path, animated=False):
	xml_file = ET.parse(xml_file_path)
	xml_root = xml_file.getroot()

	encode_data = EncodeData()
	encode_data.source_file_full_path = movie_full_path

	hdr, max_cll = __get_xml_hdr(xml_root)

	if hdr != None:
		encode_data.video_data.hdr = hdr

	if max_cll != None:
		encode_data.video_data.max_cll = max_cll

	encode_data.video_data.animated = animated

	# Streams (Video, Audio, Subtitles)
	audio_index = 0
	subtitle_index = 0 # Used to keep track of index to use in ffmpeg command for subtitles (in case more than one subtitle stream is found)
	primary_audio_language = ''
	for stream in xml_root.findall('streams/stream'):
		codec_type = stream.get('codec_type')
		if codec_type == 'video':
			width = int(stream.get('width'))
			height = int(stream.get('height'))
			encode_data.video_data.orig_resolution = f'{width}x{height}'
			resolution_val = width * height
			if resolution_val >= MIN_X265_RES_VALUE:
				encode_data.video_data.encoder = VideoEncoder.LIBX265

				color_space = stream.get('color_space')
				encode_data.video_data.color_space = color_space if color_space != None else 'bt709'

				color_transfer = stream.get('color_transfer')
				encode_data.video_data.color_transfer = color_transfer if color_transfer != None else 'bt709'

				color_primaries = stream.get('color_primaries')
				encode_data.video_data.color_primaries = color_primaries if color_primaries != None else 'bt709'

				chroma_location = stream.get('chroma_location')
				if chroma_location != None:
					if chroma_location == 'topleft':
						encode_data.video_data.chroma_location = '2'
					elif chroma_location == 'left':
						encode_data.video_data.chroma_location = '1'
					else: # Default?
						encode_data.video_data.chroma_location = '1'
			else:
				encode_data.video_data.encoder = VideoEncoder.LIBX264
		elif codec_type == 'audio':
			stream_index = int(stream.get('index'))
			codec_name = stream.get('codec_name')
			if codec_name == 'dts':
				codec_name = stream.get('profile')
			codec_priority = audio_codec_priority.get(codec_name)

			# Unknown codec; Won't know what to do with it and should be added to dictionary.
			if codec_priority == None:
				msg = f'Unknown audio codec type found. Add to dictionary. Codec Type: {codec_name}'
				return (None, msg)

			tags = stream.findall('tag')
			language = None
			title = None
			for tag in tags:
				key = tag.get('key')
				if key == 'language':
					language = tag.get('value')
				elif key == 'title':
					title = tag.get('value')
				elif (title != None) and (language != None):
					break

			# Exit if for some reason no language is found.
			if language == None:
				msg = f'Unknown language for stream index {stream_index}.'
				return (None, msg)

			commentary = False
			if (title != None) and ("Commentary" in title):
				commentary = True

			def create_audio_data(audio_index, codec_name, language, codec_priority, channels, channel_layout, title, commentary=False):
				audio_data = AudioData()
				audio_data.index = audio_index
				audio_data.descriptor = codec_name
				audio_data.language = language
				audio_data.priority = codec_priority
				audio_data.channels = channels
				audio_data.commentary = commentary

				if channel_layout != None:
					audio_data.channel_layout = channel_layout
				elif (title != None) and (commentary == False):
					audio_data.channel_layout = title
				else:
					audio_data.channel_layout = f'{channels}-channel(s)'

				if commentary == True:
					audio_data.encode_process = AudioEncodeProcess.COPY
				elif (codec_priority <= 1) and (channels <= 2):
					audio_data.encode_process = AudioEncodeProcess.AAC_STEREO
				else: 
					audio_data.encode_process = AudioEncodeProcess.COPY_WITH_AAC_STEREO

				return audio_data

			# Check if we have ANY audio streams
			if (not encode_data.audio_data) and (commentary == False):
				channels = int(stream.get('channels'))
				channel_layout = stream.get('channel_layout')
				 # audio_index should be 0 - First audio stream found
				audio_data = create_audio_data(audio_index, codec_name, language, codec_priority, channels, channel_layout, title)
				encode_data.audio_data.append(audio_data)
				primary_audio_language = language
			elif commentary == True:
				channels = int(stream.get('channels'))
				channel_layout = stream.get('channel_layout')
				audio_data = create_audio_data(audio_index, codec_name, language, codec_priority, channels, channel_layout, title, commentary)
				encode_data.audio_data.append(audio_data)
			else:
				found = False
				for i in range(0, len(encode_data.audio_data)):
					if encode_data.audio_data[i].language == language:
						found = True
						channels = int(stream.get('channels'))
						current_priority = audio_codec_priority[encode_data.audio_data[i].descriptor]
						current_channels = encode_data.audio_data[i].channels
						if (codec_priority > current_priority) or ((codec_priority == current_priority) and (channels > current_channels)):
							channel_layout = stream.get('channel_layout')
							audio_data = create_audio_data(audio_index, codec_name, language, codec_priority, channels, channel_layout, title)
							encode_data.audio_data[i] = audio_data
						else:
							break

				# Didn't find audio data with a matching language, add a new language
				if found == False:
					channels = int(stream.get('channels'))
					channel_layout = stream.get('channel_layout')
					audio_data = create_audio_data(audio_index, codec_name, language, codec_priority, channels, channel_layout, title)
					encode_data.audio_data.append(audio_data)

			audio_index += 1

		elif codec_type == 'subtitle':
			stream_index = int(stream.get('index'))
			# Only need english subtitles; Should really never have more than 2 subtitle tracks TODO: Forced subtitle tracks?
			language = None
			tags = stream.findall('tag')
			for tag in tags:
				key = tag.get('key')
				if key == 'language':
					language = tag.get('value')

			if language == 'eng':
				disposition = stream.find('disposition')
				if disposition != None:
					default = disposition.get('default')
					forced = disposition.get('forced')

					# Handle forced/default subtitle track
					if ((default == '1') or (forced == '1')) and (primary_audio_language == 'eng') :
						if encode_data.subtitle_forced_data == None:
							subtitle_forced_data = SubtitleData()
							subtitle_forced_data.language = language
							subtitle_forced_data.descriptor = stream.get('codec_name')
							subtitle_forced_data.index = subtitle_index
							subtitle_forced_data.forced = True
							encode_data.subtitle_forced_data = subtitle_forced_data

					else:
						if encode_data.subtitle_data == None:
							subtitle_data = SubtitleData()
							subtitle_data.language = language
							subtitle_data.descriptor = stream.get('codec_name')
							subtitle_data.index = subtitle_index
							encode_data.subtitle_data = subtitle_data

				else:
					msg = f'English subtitle track found with no disposition section (stream index: {stream_index}).'
					return (None, msg)

			subtitle_index += 1

		else:
			index = stream.get('index')
			msg = f'Unable to identify stream/codec type for stream {index}.'
			return (None, msg)

	format_section = xml_root.find('format')
	duration_seconds = int(float(format_section.get('duration')))
	duration_half = int(duration_seconds / 2)
	timestamp_half = convert_seconds_to_timestamp(duration_half)

	# Crop
	#crop = subprocess.check_output("""ffmpeg -i "%s" -ss %s -t 00:02:00 -vf cropdetect -f null - 2>&1 | awk '/crop/ { print $NF }' | tail -1""" % (movie_full_path, timestamp_half), shell=True, encoding='UTF-8')
	#encode_data.video_data.crop = crop.strip('\r\n')
	encode_data.video_data.crop = __get_crop(movie_full_path, timestamp_half)
	encode_data.video_data.scan = __get_scan(movie_full_path)

	msg = [f'Built encode data for {movie_full_path}'] + __build_encode_data_log_msg(encode_data)

	return (encode_data, msg)

# Creates ffmpeg command
# Only returns a msg if there is an error.
# Returns: Tuple(ffmpeg command string, encoded_file_destination_path, msg)
def build_encode_command(encode_data, source_dir, dest_dir):
	dest_path = encode_data.source_file_full_path.replace(source_dir, dest_dir, 1)
	vid_dir = dest_path.replace(os.path.basename(encode_data.source_file_full_path), '')
	msg = None

	try:
		if os.path.exists(vid_dir) == False:
			os.makedirs(vid_dir, mode=0o777)
	except Exception as error:
		dest_path = f'{dest_dir}/{os.path.basename(encode_data.source_file_full_path)}'
		msg = [f'Error creating directory {vid_dir}. Defaulting ffmpeg output to {dest_dir}.'] + traceback.format_exc().split('\n')

	audio_list = encode_data.audio_data
	video_data = encode_data.video_data
	subtitle_data = encode_data.subtitle_data
	subtitle_forced_data = encode_data.subtitle_forced_data

	# Map section
	map_str = '-map 0:v:0 '
	for audio in audio_list:
		if audio.encode_process != AudioEncodeProcess.COPY_WITH_AAC_STEREO:
			map_str += f'-map 0:a:{audio.index} '
		else:
			map_str += f'-map 0:a:{audio.index} -map 0:a:{audio.index} '
	if subtitle_data != None:
		map_str += f'-map 0:s:{subtitle_data.index} '
	if subtitle_forced_data != None:
		map_str += f'-map 0:s:{subtitle_forced_data.index} '

	# Video section
	crop = ''
	convert = ''
	# Crop
	if video_data.crop != None:
		crop = video_data.crop
	# Scan/Convert	
	if (video_data.scan != None) and (video_data.scan != VideoScan.PROGRESSIVE):
		# parity should be either 0 (TFF) or 1 (BFF) if here
		convert = f'yadif=1:{video_data.scan}:0' # mode=one frame for each field : parity : deint=all frames

	video_filter_str = ''
	if crop or convert:
		filter_str = ','.join(filter(None, [crop, convert]))
		video_filter_str = f'-vf "{filter_str}" '

	b_frames_str = 'bframes=3' if video_data.animated == False else 'bframes=8'

	if video_data.encoder == VideoEncoder.LIBX265:
		video_settings_str = (	f'-pix_fmt yuv420p10le -vcodec libx265 {video_filter_str}-preset slow '
								f'-x265-params "keyint=60:{b_frames_str}:vbv-bufsize=75000:vbv-maxrate=75000:repeat-headers=1:colorprim={video_data.color_primaries}:transfer={video_data.color_transfer}:colormatrix={video_data.color_space}')
		if video_data.hdr != None:
			hdr = video_data.hdr
			master_display_str = f":hdr10-opt=1:master-display='G({hdr.green_x},{hdr.green_y})B({hdr.blue_x},{hdr.blue_y})R({hdr.red_x},{hdr.red_y})WP({hdr.white_point_x},{hdr.white_point_y})L({hdr.max_luminance},{hdr.min_luminance})'"
			video_settings_str += master_display_str
		if video_data.max_cll != None:
			max_cll_str = f':max-cll={video_data.max_cll}'
			video_settings_str += max_cll_str
		if video_data.chroma_location != None:
			chroma_location_str = f':chromaloc={video_data.chroma_location}'
			video_settings_str += chroma_location_str

		video_settings_str += '" -crf 20 -force_key_frames "expr:gte(t,n_forced*2)" '

	elif video_data.encoder == VideoEncoder.LIBX264:
		video_settings_str = f'-pix_fmt yuv420p -vcodec libx264 {video_filter_str}-preset veryslow -x264-params "bframes=16:b-adapt=2:b-pyramid=normal:partitions=all" -crf 16 '
	else:
		error_msg = f'Invalid VideoEncoder: {video_data.encoder}. Either not handled or a bizarre issue happened.'
		msg = error_msg if msg == None else msg.append(error_msg)
		return (None, dest_path, msg)

	# Audio section
	audio_settings_str = ''
	audio_count = 0
	for audio in audio_list:
		if audio.encode_process == AudioEncodeProcess.AAC_STEREO:
			audio_settings_str += f'-c:a:{audio_count} aac -ac:a:{audio_count} 2 -b:a:{audio_count} 192k -filter:a:{audio_count} "aresample=matrix_encoding=dplii" -metadata:s:a:{audio_count} title="Stereo ({audio.language})" '
			audio_count += 1
		elif audio.encode_process == AudioEncodeProcess.COPY_WITH_AAC_STEREO:
			audio_settings_str += f'-c:a:{audio_count} copy '
			audio_count += 1
			audio_settings_str += f'-c:a:{audio_count} aac -ac:a:{audio_count} 2 -b:a:{audio_count} 192k -filter:a:{audio_count} "aresample=matrix_encoding=dplii" -metadata:s:a:{audio_count} title="Stereo ({audio.language})" '
			audio_count += 1
		elif audio.encode_process == AudioEncodeProcess.COPY:
			if audio.commentary == True:
				audio_settings_str += f'-c:a:{audio_count} copy -disposition:a:{audio_count} comment '
			else:
				audio_settings_str += f'-c:a:{audio_count} copy '
			audio_count += 1
		else: # Should never really happen
			error_msg = f'Invalid AudioEncodeProcess: {audio.encode_process}. Either not handled or a bizarre issue happened.'
			msg = error_msg if msg == None else msg.append(error_msg)
			return (None, dest_path, msg)
	
	# Subtitle section
	subtitle_settings_str = ''
	subtitle_count = 0
	if subtitle_data != None:
		subtitle_settings_str += f'-c:s:{subtitle_count} copy '
		subtitle_count += 1
	if subtitle_forced_data != None:
		subtitle_settings_str += f'-c:s:{subtitle_count} copy -disposition:s:{subtitle_count} forced '
		subtitle_count += 1

	cmd = f'ffmpeg -y -i "{encode_data.source_file_full_path}" {map_str}{video_settings_str}{audio_settings_str}{subtitle_settings_str}-max_muxing_queue_size 9999 "{dest_path}"'

	return (cmd, dest_path, msg)

### FUNCTIONS ###