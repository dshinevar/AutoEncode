from colorama import Fore, Back, Style
import copy
import os
import sys

from encode_data import *

def __printer(msg, severity, color=Fore.WHITE + Style.NORMAL):
	if isinstance(msg, list):
		print_msg = f'[{severity}] - {msg[0]}\n'
		padding = len(print_msg) - len(msg[0]) - 1
		
		for i in range(1, len(msg)):
			msg[i] = msg[i].rjust(padding + len(msg[i]), ' ') + '\n'
			print_msg += msg[i]
	else:
		print_msg = f'{color}[{severity}] - {msg}'

	print(print_msg)

def __select_video_options(video_stream_data, source_file_path):
	source_file = os.path.basename(source_file_path)
	video_details = {
		'RESOLUTION' : video_stream_data.orig_resolution,
		'CROP' : video_stream_data.crop,
		'HDR' : video_stream_data.hdr != None,
		'SCAN' : video_stream_data.scan.name
	}
	print_formatted_info(50, 'VIDEO DATA', source_file, video_details)

	info(f'Video Encoder picked from xml analyzing: {video_stream_data.encoder.name}')
	option_int = user_options([e.name for e in VideoEncoder], 'Select Video Encoder')
	encoder = VideoEncoder(option_int + 1)
	video_stream_data.encoder = encoder
	options = [False, True]
	option_int = user_options(options, 'Is this animated (anime/cartoon/etc.)?')
	video_stream_data.animated = options[option_int]

	return video_stream_data

def __select_audio_options(audio_streams_list, source_file_path):
	source_file = os.path.basename(source_file_path)
	name = (source_file[:42] + '..') if len(source_file) > 44 else source_file
	formatted_stream_list = []
	for stream in audio_streams_list:
		audio_str = f'{stream.descriptor} {stream.channel_layout} ({stream.language})'
		formatted_stream_list.append(audio_str)

	audio_streams = {}
	for i in range(0, len(formatted_stream_list)):
		audio_streams[f'STREAM {i + 1}:'] = formatted_stream_list[i]
	print_formatted_info(50, 'AUDIO STREAMS AVAILABLE', source_file, audio_streams, False)

	done_int = len(formatted_stream_list)
	audio_data_list = []
	while True:
		if audio_data_list:
			print_formatted_info(50, 'AUDIO STREAMS AVAILABLE', source_file, audio_streams, False)
			selected_streams = {}
			for i in range(0, len(audio_data_list)):
				audio = audio_data_list[i]
				selected_streams[f'{i + 1}:'] = f'{audio.descriptor} {audio.channel_layout} ({audio.language}) => {audio.encode_process.name}'
			print_formatted_info(60, 'AUDIO STREAMS SELECTED', info=selected_streams, info_right_justify=False)

		stream_int = user_options(formatted_stream_list + ['DONE'], 'Select Audio Stream to use')
		if stream_int == done_int:
			if not audio_data_list:
				error('No audio streams selected/used.', False)
				continue
			else:
				break

		info(f'Selected Stream: {formatted_stream_list[stream_int]}')
		option_int = user_options([e.name for e in AudioEncodeProcess], 'Select Audio Encode Process to use on stream')
		encode_process = AudioEncodeProcess(option_int + 1)

		temp_list = copy.deepcopy(audio_streams_list)
		audio_data = temp_list[stream_int]
		audio_data.encode_process = encode_process
		audio_data_list.append(audio_data)

	return audio_data_list

def __select_subtitle_options(subtitle_streams_list, source_file_path):
	source_file = os.path.basename(source_file_path)
	formatted_subtitle_list = []
	formatted_subtitle_forced_list = []
	subtitle_list = []
	subtitle_forced_list = []
	for stream in subtitle_streams_list:
		subtitle_str = f'{stream.descriptor} ({stream.language})'
		if stream.forced == True:
			subtitle_str += ' forced'
			subtitle_forced_list.append(stream)
			formatted_subtitle_forced_list.append(subtitle_str)
		else:
			subtitle_list.append(stream)
			formatted_subtitle_list.append(subtitle_str)

	subtitles = {}
	subtitles_forced = {}
	for i in range(0, len(formatted_subtitle_list)):
		subtitles[f'STREAM {i + 1}:'] = f'{formatted_subtitle_list[i]}'
	for i in range(0, len(formatted_subtitle_forced_list)):
		subtitles_forced[f'STREAM {i + 1}:'] = f'{formatted_subtitle_forced_list[i]}'
	print_formatted_info(50, 'SUBTITLE STREAMS', source_file, subtitles, False)
	print_formatted_info(50, 'FORCED SUBTITLE STREAMS', source_file, subtitles_forced, False)

	selected_subtitle = None
	selected_forced_subtitle = None
	if subtitle_list:
		option_int = user_options(formatted_subtitle_list, 'Select Subtitle Stream to Copy')
		selected_subtitle = subtitle_list[option_int]

	if subtitle_forced_list:
		option_int = user_options(formatted_subtitle_forced_list, 'Select Forced Subtitle Stream to Copy')
		selected_forced_subtitle = subtitle_forced_list[option_int]

	return (selected_subtitle, selected_forced_subtitle)

# Basic user options handler.
# Spins in a loop as long as invalid options are given
def user_options(options, header=None):
	option_int = None
	while option_int == None:
		if header != None:
			print('[OPTION] - ' + header)

		for i in range(0, len(options)):
			print(f'  {i + 1}) {options[i]}')

		option = input('Select option: ')

		if option.isnumeric():
			option_int = int(option)
		else:
			print('INVALID INPUT OR INVALID OPTION SELECTED.')
			option_int = None
			continue

		if (1 <= option_int <= len(options)):
			return (option_int - 1)
		else:
			print('INVALID INPUT OR INVALID OPTION SELECTED.')
			option_int = None

# Description: Guides user through selecting show season/single episode
# Returns: Either a list of episode paths if a whole season was selected
# Or a single episode path if a single episode was selected.
def select_show_season_episode(show_path):
	seasons_list = list(next(os.walk(show_path))[1])
	seasons_list.sort()
	info(f'Found {len(seasons_list)} season(s).')

	options = ['Whole Season', 'Single Episode']
	selected_option_int = user_options(options, 'Run ffmpeg for a whole season or a single episode?')
	selected_option = options[selected_option_int]

	header = 'Select Season' if selected_option == 'Whole Season' else 'Select Season To Select Episode From'
	selected_option_int = user_options(seasons_list, header)

	season = seasons_list[selected_option_int]
	season_path = f'{show_path}/{season}'
	episode_list = list(next(os.walk(season_path))[2])
	episode_list.sort()

	if selected_option == 'Whole Season':
		for i in range(0, len(episode_list)):
			episode_list[i] = f'{season_path}/{episode_list[i]}'
		return episode_list
	else:
		selected_option_int = user_options(episode_list, 'Select Episode')
		episode = episode_list[selected_option_int]
		episode_path = f'{season_path}/{episode}'
		return episode_path

def select_encoding_options(file_path):
	msg = None
	xml_file_path, msg = create_video_data_xml(file_path)

	if xml_file_path == None:
		return (None, msg)

	info(msg)
	info('Building Stream Data')

	stream_data = build_stream_data(xml_file_path, file_path)
	encode_data = EncodeData()
	encode_data.source_file_full_path = file_path
	encode_data.video_data = __select_video_options(stream_data.video_stream, stream_data.source_file_full_path)
	encode_data.audio_data = __select_audio_options(stream_data.audio_streams, stream_data.source_file_full_path)
	subtitle, subtitle_forced = __select_subtitle_options(stream_data.subtitle_streams, stream_data.source_file_full_path)
	encode_data.subtitle_data = subtitle
	encode_data.subtitle_forced_data = subtitle_forced

	# DELETE XML
	try:
		os.remove(xml_file_path)
	except OSError as error:
		error(f'Error deleting {xml_file_path}', False)
		print(error)
	else:
		info(f'Deleted {xml_file_path}')

	return (encode_data, msg)

# Print info function
# Use complete when wanting to signal information about something
# completing.
def info(msg, complete=False):
	color = Fore.GREEN + Style.BRIGHT if complete else Fore.CYAN + Style.BRIGHT
	__printer(msg, 'INFO', color)

# Print warning message
def warning(msg):
	__printer(msg, 'WARNING', Fore.YELLOW + Style.BRIGHT)

# Print error message
# Use exit for when wanting to close the program after error (FATAL)
def error(msg, exit=True):
	__printer(msg, 'ERROR', Fore.RED)

	if exit == True:
		sys.exit(1)

def print_formatted_info(width, header, subheader=None, info=None, info_right_justify=True):
	width = 10 if width < 10 else width
	print_str = ''
	header_str = (header[:(width - 6)] + '..') if len(header) > (width - 4) else header
	print_str += f'\n{f" {header_str} ":#^{width}}\n'

	if subheader != None:
		subheader_str = (subheader[:(width - 8)] + '..') if len(subheader) > (width - 6) else subheader
		print_str += f'{f" {subheader_str} ":#^{width}}\n'

	if info != None:
		if isinstance(info, dict):
			for title, data in info.items():
				title_len = len(title) + 2
				data_len = width - title_len
				if info_right_justify:
					print_str += f'{f"# {title}":<{title_len}}{f"{data} #":>{data_len}}\n'
				else:
					print_str += f'{f"# {title} ":<{title_len}}{f"{data}":<{data_len - 3}} #\n'

	print_str += f'{"":#>{width}}\n'

	print(print_str)
