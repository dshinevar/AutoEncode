import configparser
from datetime import datetime as dt
from enum import Enum
import os
import pytz
import shutil
from signal import *
import subprocess
import sys
import time

import automated_ffmpeg_utils as utils
import plex_interactor

# GLOBALS
ffmpeg_proc = None
timezone = None

config_name = 'automated_ffmpeg_config.ini'
log_file = ''
current_working_movie = None
working_movie_path = '/tmp/automated_ffmpeg/working_movie.txt' 
# GLOBALS

class Severity(Enum):
	INFO = 1
	ERROR = 2

def log(severity, msg):
	time_str = dt.now(timezone).strftime('%m/%d/%y %H:%M:%S')

	if isinstance(msg, list):
		log_msg = '[%s] - [%s] : %s\n' % (time_str, severity.name, msg[0])
		padding = len(log_msg) - len(msg[0]) - 1
		
		for i in range(1, len(msg)):
			msg[i] = msg[i].rjust(padding + len(msg[i]), ' ') + '\n'
			log_msg += msg[i]
	else:
		log_msg = '[%s] - [%s] : %s\n' % (time_str, severity.name, msg)

	with open(log_file, 'a') as f:
		f.write(log_msg)

def exit_cleanup(*args):
	if current_working_movie == None:
		log(Severity.INFO, 'Program exited/terminated. Cleaning up.')
	else:
		msg = ['Program exited/terminated. Cleaning up.',
			'Moving being encoded when terminated: %s' % current_working_movie]
		log(Severity.INFO, msg)

	if ffmpeg_proc != None:
		ffmpeg_proc.kill()

	sys.exit(1)

def main():
	global ffmpeg_proc
	global log_file
	global timezone
	global current_working_movie
	config = configparser.ConfigParser()
	config.read(config_name)
	tz = config['DEFAULT'].get('timezone', 'US/Central')
	
	timezone = pytz.timezone(tz)

	if os.access('/var/log/', os.W_OK) == True:
		if os.path.exists('/var/log/automated_ffmpeg') == False:
			os.makedirs('/var/log/automated_ffmpeg')

		log_file = '/var/log/automated_ffmpeg/automated_ffmpeg_log.txt'

	else:
		if os.path.exists('/tmp/automated_ffmpeg') == False:
			os.makedirs('/tmp/automated_ffmpeg')

		log_file = '/tmp/automated_ffmpeg/automated_ffmpeg_log.txt'

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
		msg = ['Error deleting previous working movie or working_movie file.', error]
		log(Severity.ERROR, msg)

	# Config - Directory Info
	try:
		directories = config['Directories']
		movie_dirs = directories['movie'].split(',')
		movie_encoded_dirs = directories['movie_encoded'].split(',')
		plex_dirs = directories['plex'].split(',')
		plex_sections = directories['plex_section'].split(',')
	except Exception as error:
		msg = ['Error getting directory info from config file. Exiting.', error]
		log(Severity.ERROR. msg)
		sys.exit(1)

	# Config - Plex Info
	try:
		plex_username = config['Plex']['username']
		plex_password = config['Plex']['password']
		plex_servername = config['Plex']['server']
	except Exception as error:
		msg = ['Error getting Plex info from config file. Exiting.', error]
		log(Severity.Error, msg)
		sys.exit(1)

	min_len = len(min(movie_dirs, movie_encoded_dirs, plex_dirs, plex_sections, key=len))
	max_len = len(max(movie_dirs, movie_encoded_dirs, plex_dirs, plex_sections, key=len))

	if min_len < 1:
		log(Severity.ERROR, 'Length of one of the directory lists in config file is zero. Exiting.')
		sys.exit(1)
	elif min_len != max_len:
		msg = ['Issue with length of given directory lists.  Check config file.  Will proceed using minimum list length.',
			'Minimum Directory List Length: %d' % min_len,
			'Maximum Directory List Length: %d' % max_len]
		log(Severity.ERROR, msg)

	plex_interact = plex_interactor.PlexInteractor(plex_username, plex_password, plex_servername)

	ffmpeg_version = subprocess.check_output('ffmpeg -version', encoding='UTF-8', shell=True).split('\n')

	msg = ['AUTOMATED_FFMPEG START UP/INITIALIZED.',
		'TIMEZONE: %s' % tz,
		'MOVIE DIRECTORIES: %s' % movie_dirs[:min_len],
		'MOVIE ENCODED DIRECTORIES: %s' % movie_encoded_dirs[:min_len],
		'PLEX DIRECTORIES: %s' % plex_dirs[:min_len],
		'PLEX LIBRARY SECTIONS: %s' % plex_sections[:min_len],
		'PLEX SERVER: %s' % plex_servername,
		'FFMPEG VERSION INFO:'] + ffmpeg_version
	log(Severity.INFO, msg)

	# Get into what should be a never ending loop
	while True:
		found_movies_to_encode = False
		for i in range(0, min_len):

			movie_files, movie_files_base = utils.build_movie_lists(movie_dirs[i])
			movie_encoded_files, movie_encoded_files_base = utils.build_movie_encoded_lists(movie_encoded_dirs[i])
			to_encode = utils.build_to_encode_list(movie_files, movie_files_base, movie_encoded_files_base)

			if len(to_encode) > 0:
				log(Severity.INFO, 'Found %d new movie(s) to encode.' % len(to_encode))
				found_movies_to_encode = True
				# Encode each new movie
				for movie in to_encode:
					try:
						# FILE READY CHECK
						is_ready, msg = utils.check_file_ready(movie)
						# If not ready, move on to next movie
						if is_ready == False:
							log(Severity.ERROR, msg)
							continue

						# CREATE XML
						xml_file_path, msg = utils.create_video_data_xml(movie)
						if xml_file_path == None:
							log(Severity.ERROR, msg)
							continue
						else:
							log(Severity.INFO, msg)

						# BUILD ENCODE DATA
						encode_data, msg = utils.build_encode_data(movie, xml_file_path)
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
							msg = ['Error deleting %s' % xml_file_path, error]
							log(Severity.ERROR, msg)
						else:
							log(Severity.INFO, 'Deleted %s' % xml_file_path)

						# BUILD COMMAND
						cmd, encoded_movie_path = utils.build_encode_command(encode_data, movie_encoded_dirs[i])

						msg = ['STARTING ENCODING FOR: %s' % movie, 'FFMPEG CMD: %s' % cmd]
						log(Severity.INFO, msg)

						start_time = dt.now()
						current_working_movie = movie
						with open(working_movie_path, 'w') as f:
							f.write(encoded_movie_path)

						ffmpeg_proc = subprocess.run('exec ' + cmd, shell=True, stderr=subprocess.PIPE)

						stop_time = dt.now()
						current_working_movie = None

						if ffmpeg_proc.returncode != 0:
							error_msg = ffmpeg_proc.stderr.decode('utf-8').split('\n')
							msg = 'Error running ffmpeg for %s. Details below' % movie
							error_msg.insert(0, msg)
							log(Severity.ERROR, error_msg)
							ffmpeg_proc = None

							try:
								os.remove(working_movie_path)
							except Exception as error:
								msg = ['Error deleting /tmp/automated_ffmpeg/working_movie.txt', error]
								log(Severity.ERROR, msg)
						else:
							msg = ['COMPLETED ENCODING FOR %s' % movie, 'Time Elapsed: %s' % str(stop_time - start_time)]
							log(Severity.INFO, msg)
							ffmpeg_proc = None

							try:
								os.remove(working_movie_path)
							except Exception as error:
								msg = ['Error deleting /tmp/automated_ffmpeg/working_movie.txt', error]
								log(Severity.ERROR, msg)

							# COPY FILE OVER TO PLEX MEDIA DIRECTORIES
							# Don't need to delete file as this should be a new movie
							# Get encoded_movie_path from building encode command
							try:
								shutil.copy2(encoded_movie_path, plex_dirs[i])
							except IOError as error:
								msg = ['Error copying %s to %s (Details below). Will not attempt to update plex server.' % (encoded_movie_path, plex_dirs[i]), error]
								log(Severity.ERROR, msg)
								continue
							else:
								log(Severity.INFO, 'Successfully copied %s to %s' % (encoded_movie_path, plex_dirs[i]))

							plex_interact.update(plex_sections[i])

					except Exception as error:
						msg = ['Error during processing/encoding %s' % movie, str(error)]
						log(Severity.ERROR, msg)

		if found_movies_to_encode == False:
			movie_files = []
			movie_files_base = []
			movie_encoded_files = []
			movie_encoded_files_base = []
			time.sleep(1800) # 30 minutes

if __name__ == "__main__":
	main()