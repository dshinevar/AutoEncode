from glob import glob
import os
from pathlib import Path

# Builds a list of movie files and their base filename based on the given directory
# and/or a user_input.  Assumes user_input is None if not given.
# Returns: Tuple with index 0 being movie files, and index 1 being base filenames
def build_movie_lists(source_dir, user_input=None):
	movie_files = []
	movie_files_base = []

	full_search = f'{source_dir}/**/*.mkv' if user_input == None else f'{source_dir}/**/*{user_input}*.mkv'

	for file in glob(full_search, recursive=True):
		movie_files.append(file)
		movie_files_base.append(Path(file).stem)

	movie_files.sort()
	movie_files_base.sort()

	return (movie_files, movie_files_base)

def build_show_list_with_input(source_dir, user_input):
	shows = []
	shows_base = []

	full_search = f'{source_dir}/*{user_input}*'

	for show in glob(full_search):
		shows.append(show)
		shows_base.append(os.path.basename(show))

	shows.sort()
	shows_base.sort()

	return (shows, shows_base)

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
		movie_encoded_files.extend(glob(f'{movie_encoded_dir}/**/{file}', recursive=True))

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
	to_encode = [m for m in movie_files if any(f'{sub}.mkv' in m for sub in diff_list)]

	return to_encode