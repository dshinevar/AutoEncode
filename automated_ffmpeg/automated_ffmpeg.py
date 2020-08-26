from configparser import ConfigParser
from datetime import datetime as dt
import os
import pytz
import shutil
from signal import *
from stat import *
import subprocess
import sys
import time
import traceback

from encode_data import *
from ffmpeg_tools_utilities import *
from list_builders import *
from plex_interactor import *
from simple_logger import *

### GLOBALS ###
ffmpeg_proc = None
logger = None

config_name = '/usr/local/bin/automated_ffmpeg_config.ini'
current_working_movie = None
working_movie_path = '/tmp/automated_ffmpeg/working_movie.txt' 
### GLOBALS ###

### FUNCTIONS ###
def log(severity, msg):
	global logger
	logger.log(severity, msg)

def exit_cleanup(*args):
	global logger
	global ffmpeg_proc
	global current_working_movie
	if logger != None:
		if current_working_movie == None:
			log(Severity.INFO, 'Program exited/terminated. Cleaning up.')
		else:
			msg = ['Program exited/terminated. Cleaning up.',
				f'Movie being encoded when terminated: {current_working_movie}']
			log(Severity.INFO, msg)

	if ffmpeg_proc != None:
		ffmpeg_proc.kill()

	sys.exit(0)

### FUNCTIONS ###

### MAIN ###
config = ConfigParser()
config.read(config_name)
tz = config['Logger'].get('timezone', 'US/Central')
timezone = pytz.timezone(tz)
max_bytes = config['Logger'].getint('max_bytes', 256000) # Defaults to 250 KB
backup_count = config['Logger'].getint('backup_count', 3)

if os.path.exists('/tmp/automated_ffmpeg') == False:
		os.makedirs('/tmp/automated_ffmpeg')

if os.access('/var/log/', os.W_OK) == True:
	if os.path.exists('/var/log/automated_ffmpeg') == False:
		os.makedirs('/var/log/automated_ffmpeg')

	log_file = '/var/log/automated_ffmpeg/automated_ffmpeg.log'

else:
	log_file = '/tmp/automated_ffmpeg/automated_ffmpeg.log'

logger = SimpleLoggerWithRollover(timezone, log_file, max_bytes=max_bytes, backup_count=backup_count)

# Set cleanup function for potential termination
for sig in (SIGABRT, SIGALRM, SIGBUS, SIGILL, SIGINT, SIGTERM):
	signal(sig, exit_cleanup)

try:
	if os.path.exists(working_movie_path):
		with open(working_movie_path, 'r') as f:
			file_to_delete = f.readline()
			if os.path.exists(file_to_delete):
				os.remove(file_to_delete)
		os.remove(working_movie_path)
except Exception as error:
	msg = ['Error deleting previous working movie or working_movie file.'] + traceback.format_exc().split('\n')
	log(Severity.ERROR, msg)

plex_enabled = config['DEFAULT'].getboolean('plex_enabled', False)

# Config - Directory Info
try:
	directories = config['Directories']
	movie_dirs = directories['movie'].split(',')
	movie_encoded_dirs = directories['movie_encoded'].split(',')

	if plex_enabled == True:
		plex_dirs = directories['plex'].split(',')
		plex_sections = directories['plex_section'].split(',')
except Exception as error:
	msg = ['Error getting directory info from config file. Exiting.'] + traceback.format_exc().split('\n')
	log(Severity.FATAL, msg)
	sys.exit(1)

# Config - Plex Info
if plex_enabled == True:
	try:
		plex_username = config['Plex']['username']
		plex_password = config['Plex']['password']
		plex_servername = config['Plex']['server']

		plex_interact = PlexInteractor(plex_username, plex_password, plex_servername)

		min_len = len(min(movie_dirs, movie_encoded_dirs, plex_dirs, plex_sections, key=len))
		max_len = len(max(movie_dirs, movie_encoded_dirs, plex_dirs, plex_sections, key=len))

	except Exception as error:
		msg = ['Error getting Plex info from config file. Exiting.'] + traceback.format_exc().split('\n')
		log(Severity.FATAL, msg)
		sys.exit(1)
else:
	min_len = len(min(movie_dirs, movie_encoded_dirs, key=len))
	max_len = len(max(movie_dirs, movie_encoded_dirs, key=len))

if min_len < 1:
	log(Severity.FATAL, 'Length of one of the directory lists in config file is zero. Exiting.')
	sys.exit(1)
elif min_len != max_len:
	msg = ['Issue with length of given directory lists.  Check config file.  Will proceed using minimum list length.',
		f'Minimum Directory List Length: {min_len}',
		f'Maximum Directory List Length: {max_len}']
	log(Severity.ERROR, msg)

ffmpeg_version = subprocess.check_output('ffmpeg -version', encoding='UTF-8', shell=True).split('\n')

msg = ['AUTOMATED_FFMPEG INITIALIZED.',
	f'TIMEZONE: {tz}',
	f'MOVIE DIRECTORIES: {movie_dirs[:min_len]}',
	f'MOVIE ENCODED DIRECTORIES: {movie_encoded_dirs[:min_len]}']

if plex_enabled == True:
	msg += [f'PLEX DIRECTORIES: {plex_dirs[:min_len]}',
	f'PLEX LIBRARY SECTIONS: {plex_sections[:min_len]}',
	f'PLEX SERVER: {plex_servername}']

msg += ['FFMPEG VERSION INFO:'] + ffmpeg_version
log(Severity.INFO, msg)

sleep_time = config['DEFAULT'].getint('sleep', 1800) # Defaults to 30 minutes

# Get into what should be a never ending loop
while True:
	found_movies_to_encode = False
	for i in range(0, min_len):

		movie_files, movie_files_base = build_movie_lists(movie_dirs[i])

		if (not movie_files) or (not movie_files_base):
			msg = f'No movies found in {movie_dirs[i]}.'
			log(Severity.ERROR, msg)
			continue

		movie_encoded_files, movie_encoded_files_base = build_movie_encoded_lists(movie_encoded_dirs[i])
		to_encode = build_to_encode_list(movie_files, movie_files_base, movie_encoded_files_base)

		if len(to_encode) > 0:
			msg = [f'Found {len(to_encode)} new movie(s) to encode.'] + [os.path.basename(movie) for movie in to_encode]
			log(Severity.INFO, msg)
			found_movies_to_encode = True
			# Encode each new movie
			for movie in to_encode:
				try:
					# FILE READY CHECK
					is_ready, msg = check_file_ready(movie)
					# If not ready, move on to next movie
					if is_ready == False:
						log(Severity.ERROR, msg)
						# If any movies come after this one, it'll flip back to true.
						# If this is the last movie/only movie, it needs more time so 
						# force the script to sleep.
						found_movies_to_encode = False
						continue

					# CREATE XML
					xml_file_path, msg = create_video_data_xml(movie)
					if xml_file_path == None:
						log(Severity.ERROR, msg)
						continue
					else:
						log(Severity.INFO, msg)

					# BUILD ENCODE DATA
					encode_data, msg = build_encode_data(movie, xml_file_path)
					if encode_data == None:
						log(Severity.ERROR, msg)
						# Don't delete xml file here in case it needs to be looked at
						continue
					else:
						log(Severity.INFO, msg)

					# DELETE XML
					try:
						os.remove(xml_file_path)
					except OSError as error:
						msg = [f'Error deleting {xml_file_path}', error]
						log(Severity.ERROR, msg)
					else:
						log(Severity.INFO, f'Deleted {xml_file_path}')

					# BUILD COMMAND
					cmd, encoded_movie_path, msg = build_encode_command(encode_data, movie_dirs[i], movie_encoded_dirs[i])
					if msg != None:
						log(Severity.ERROR, msg)

					msg = [f'STARTING ENCODING FOR: {movie}', f'FFMPEG CMD: {cmd}']
					log(Severity.INFO, msg)

					current_working_movie = movie
					with open(working_movie_path, 'w') as f:
						f.write(encoded_movie_path)
					start_time = dt.now()

					ffmpeg_proc = subprocess.run('exec ' + cmd, shell=True, stderr=subprocess.PIPE)

					stop_time = dt.now()
					current_working_movie = None

					if ffmpeg_proc.returncode != 0:
						error_msg = ffmpeg_proc.stderr.decode('utf-8').split('\n')
						msg = f'Error running ffmpeg for {movie}. Details below'
						error_msg.insert(0, msg)
						log(Severity.ERROR, error_msg)
						ffmpeg_proc = None

						try:
							os.remove(working_movie_path)
						except Exception as error:
							msg = [f'Error deleting {working_movie_path}'] + traceback.format_exc().split('\n')
							log(Severity.ERROR, msg)
					else:
						elapsed_time = stop_time - start_time
						msg = [f'COMPLETED ENCODING FOR {movie}', f'Time Elapsed: {str(elapsed_time)}']
						log(Severity.INFO, msg)
						ffmpeg_proc = None

						try:
							os.remove(working_movie_path)
						except Exception as error:
							msg = [f'Error deleting {working_movie_path}'] + traceback.format_exc().split('\n')
							log(Severity.ERROR, msg)

						# PLEX INTERACT SECTION
						if plex_enabled == True:
							# COPY FILE OVER TO PLEX MEDIA DIRECTORIES
							# Get encoded_movie_path from building encode command
							encoded_movie_plex_dest = encoded_movie_path.replace(movie_encoded_dirs[i], plex_dirs[i]).replace(os.path.basename(encoded_movie_path), '')
							try:
								if os.path.exists(encoded_movie_plex_dest) == False:
									os.makedirs(encoded_movie_plex_dest)

								shutil.copy2(encoded_movie_path, encoded_movie_plex_dest)
							except Exception as error:
								msg = [f'Error copying {encoded_movie_path} to {encoded_movie_plex_dest} (Details below). Will not attempt to update plex server.'] + traceback.format_exc().split('\n')
								log(Severity.ERROR, msg)
								continue
							else:
								log(Severity.INFO, f'Successfully copied {encoded_movie_path} to {encoded_movie_plex_dest}')

							try:
								plex_interact.update(plex_sections[i])
							except Exception as error:
								msg = [f'Failed to update Plex Server.', f'Server: {plex_servername}', f'Section: {plex_sections[i]}'] + traceback.format_exc().split('\n')
								log(Severity.ERROR, msg)

				except Exception as error:
					msg = [f'Error during processing/encoding {movie}'] + traceback.format_exc().split('\n')
					log(Severity.ERROR, msg)

	if found_movies_to_encode == False:
		movie_files = []
		movie_files_base = []
		movie_encoded_files = []
		movie_encoded_files_base = []
		to_encode = []
		try:
			logger.check_rollover()
		except Exception as error:
			msg = ['Error doing log file rollover.'] + traceback.format_exc().split('\n')
			log(Severity.ERROR, msg)

		time.sleep(sleep_time) # Wait a while before checking for more work

### MAIN ###