from colorama import init
from configparser import ConfigParser
from datetime import datetime as dt
from getopt import getopt, GetoptError
import os
from signal import *
import shutil
from stat import *
from subprocess import Popen
import subprocess
import sys
import traceback
import xml.etree.ElementTree as ET

from encode_data import *
from ffmpeg_tools_utilities import *
from list_builders import *
from plex_interactor import *
from user_options import *

### GLOBALS ###
class VideoType(Enum):
	MOVIE = 1
	SHOW = 2

config_path = './ffmpeg_guided_config.ini'

config_lookup = {
	'-m' : ('movie_dirs', VideoType.MOVIE, 'Movie'),
	'--movie' : ('movie_dirs', VideoType.MOVIE, 'Movie'),
	'-t' : ('tv_show_dirs', VideoType.SHOW, 'TV Show'),
	'--tv' : ('tv_show_dirs', VideoType.SHOW, 'TV Show'),
	'-a' : ('anime_dirs', VideoType.SHOW, 'Anime'),
	'--anime' : ('anime_dirs', VideoType.SHOW, 'Anime'),
	'-am' : ('anime_movie_dirs', VideoType.MOVIE, 'Anime Movie'),
	'--anime_movie' : ('anime_movie_dirs', VideoType.MOVIE, 'Anime Movie')
}

current_working_file = None
### GLOBALS ###
### FUNCTIONS ###
def usage():
	print(f'Usage: python3 {sys.argv[0]} [-m movie | -t tv_show | -a anime | -am anime_movie]')

def run_encode_command(cmd, current_file):
	global current_working_file

	current_working_file = current_file
	start_time = dt.now()
	map_flag = False
	with subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, bufsize=1, universal_newlines=True, shell=True) as p:
		for line in p.stdout:
			print_line = line.strip('\n')
			if 'Stream mapping:' in line:
				print(print_line)
				map_flag = True
			elif map_flag and 'Stream #' in line:
				print(print_line)
			elif map_flag and 'Stream #' not in line:
				map_flag = False
			elif line.startswith('frame='):
				print(print_line, end='\r', flush=True)
			else:
				continue

	stop_time = dt.now()
	current_working_file = None

	if p.returncode != 0:
		error_msg = p.stderr.decode('utf-8')
		error(f'Error running ffmpeg for {current_file}. Details below.', False)
		error(error_msg, False)
		p = None
		return None

	elapsed_time = stop_time - start_time
	return elapsed_time

def copy_to_plex(file, dest_dir, plex_dir):
	copy_count = 0
	plex_dest = file.replace(dest_dir, plex_dir).replace(os.path.basename(file), '')
	try:
		os.umask(0)
		os.makedirs(plex_dest, mode=0o777, exist_ok=True)
	except Exception as error:
		error(f'Failed to create plex directory: {plex_dest}. Not copying to plex directory.', False)
		return copy_count

	try:
		shutil.copy2(file, plex_dest)
		copy_count += 1
	except Exception as error:
		msg = [f'Error copying {file} to {plex_dest} (Details below).'] + traceback.format_exc().split('\n')
		error(msg, False)
	else:
		info(f'Successfully copied {file} to {plex_dest}')

	return copy_count

def copy_to_plex_list(file_list, dest_dir, plex_dir):
	copy_count = 0
	plex_dest = file_list[0].replace(dest_dir, plex_dir).replace(os.path.basename(file_list[0]), '')
	try:
		os.umask(0)
		os.makedirs(plex_dest, mode=0o777, exist_ok=True)
	except Exception as error:
		error(f'Failed to create plex directory: {plex_dest}. Not copying to plex directory.', False)
		return copy_count

	for file in file_list:
		try:
			shutil.copy2(file, plex_dest)
			copy_count += 1
		except Exception as error:
			msg = [f'Error copying {file} to {plex_dest} (Details below).'] + traceback.format_exc().split('\n')
			error(msg, False)
			continue
		else:
			info(f'Successfully copied {file} to {plex_dest}')

	return copy_count

def exit_cleanup(*args):
	global current_working_file

	if current_working_file == None:
		print('\n## Program exited/terminated. Cleaning up. ##')
	else:
		print(f'\n## Program exited/terminated. Cleaning up. File being encoded when terminated: {current_working_file} ##')

	sys.exit(0)
### FUNCTIONS ###

### MAIN ###
init(autoreset=True)
argv = sys.argv[1:]

# Need at least one argument
if (len(argv) < 2) or (len(argv) > 3):
	usage()
	sys.exit(2)

try:
	opts, args = getopt(argv, "m:t:a:am:", ["movie=", "tv=", "anime=", "anime_movie="])

	# Should never happen
	if len(opts) > 1:
		usage()
		sys.exit(2)

	opt, arg = opts[0]

except GetoptError:
	usage()
	sys.exit(2)

# Set cleanup function for potential termination
for sig in (SIGABRT, SIGALRM, SIGBUS, SIGILL, SIGINT, SIGTERM):
	signal(sig, exit_cleanup)

config = ConfigParser()
config.read(config_path)

plex_enabled = config['DEFAULT'].getboolean('plex_enabled', False)

config_field, video_type, option_str = config_lookup.get(opt, None)
if config_field == None:
	error(f'Unable to find directories in {config_path} for option {opt}. Exiting.')

directories = config['Directories'][config_field].split(',')

if plex_enabled == True:
	if len(directories) != 4:
		error(f'Expected 4 entries in config field {config_field}. Found {len(directories)}.')

	source_dir = directories[0]
	destination_dir = directories[1]
	plex_dir = directories[2]
	plex_section = directories[3]

	try:
		plex_baseurl = config['Plex']['baseurl']
		plex_token = config['Plex']['token']

		plex_interact = PlexInteractor(plex_baseurl, plex_token)
	except Exception as error:
		msg = ['Error getting Plex info from config file. Exiting.'] + traceback.format_exc().split('\n')
		error(msg)

else:
	if len(directories) < 2:
		error(f'Expected at least 2 entries in config field {config_field}. Found {len(directories)}')

	source_dir = directories[0]
	destination_dir = directories[1]

user_info = {
	'OPTION SELECTED' : option_str,
	'PLEX ENABLED' : plex_enabled
}
print_formatted_info(50, 'FFMPEG GUIDED', 'Developed By: Daniel Shinevar', user_info)

if video_type == VideoType.MOVIE:
	movie_list, movie_list_base = build_movie_list(source_dir, arg)
	selected_option = user_options(movie_list_base, 'Select Movie')
elif video_type == VideoType.SHOW:
	shows_list, shows_list_base = build_show_list_with_input(source_dir, arg)
	selected_option = user_options(shows_list_base, 'Select Show')
	selected_show = shows_list[selected_option]
	selected_show_base = shows_list_base[selected_option]
	selected_option = select_show_season_episode(selected_show)

	if isinstance(selected_option, list):
		episodes = selected_option
		warning('Encode data options based off of a single episode')
		is_ready, msg = check_file_ready(episodes[0])
		if is_ready == False:
			error(msg)
		info('First episode is ready.')
		encode_data, msg = select_encoding_options(episodes[0])
		if encode_data == None:
			if msg != None:
				error(msg)

		encoded_file_path_list = []

		for episode in episodes:
			is_ready, msg = check_file_ready(episode)
			if is_ready == False:
				error(msg, False)
				continue

			encode_data.source_file_full_path = episode
			cmd, encoded_file_path, msg = build_encode_command(encode_data, source_dir, destination_dir)
			if cmd == None:
				error(msg, False)
				continue
			elif msg != None:
				error(msg, False)

			info(f'STARTED ENCODING FOR {episode}')
			info(f'CMD USED: {cmd}\n')
			elapsed_time = run_encode_command(cmd, episode)
			if elapsed_time != None:
				encoded_file_path_list.append(encoded_file_path)
				info(f'COMPLETED ENCODING FOR {episode}', complete=True)
				info(f'TIME ELAPSED: {str(elapsed_time)}')

		info(f'COMPLETED ENCODING WHOLE SEASON', complete=True)

		if plex_enabled == True:
			copy_count = copy_to_plex_list(encoded_file_path_list, destination_dir, plex_dir)
			# If the returned count does not equal the length of the list passed, something went
			# wrong and we won't update plex
			if copy_count == len(encoded_file_path_list):
				info(f'Successfully copied all files to plex destination.', complete=True)
				try:
					plex_interact.update(plex_section)
				except Exception as error:
					msg = [f'Failed to update Plex Server.', f'Server URL: {plex_baseurl}', f'Section: {plex_section}'] + traceback.format_exc().split('\n')
					error(msg)
				else:
					msg = f'Updated Plex Server. Server URL: {plex_baseurl} | Section: {plex_section}'
					info(msg)
			else:
				error(f'Only {copy_count} out of {len(encoded_file_path_list)} files copied. Plex will not be updated.', False)

	else:
		episode = selected_option
		is_ready, msg = check_file_ready(episode)
		if is_ready == False:
			error(msg)

		info(f'Episode {episode} is ready.')
		encode_data, msg = select_encoding_options(episode)
		if encode_data == None:
			if msg != None:
				error(msg)

		cmd, encoded_file_path, msg = build_encode_command(encode_data, source_dir, destination_dir)
		if cmd == None:
			error(msg)
		elif msg != None:
			error(msg, False)

		info(f'STARTED ENCODING FOR {episode}')
		info(f'CMD USED: {cmd}\n')
		elapsed_time = run_encode_command(cmd, episode)
		if elapsed_time != None:
			info(f'COMPLETED ENCODING FOR {episode}', complete=True)
			info(f'TIME ELAPSED: {str(elapsed_time)}', complete=True)

sys.exit(0)

### MAIN ###
	