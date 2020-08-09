from glob import glob
from pathlib import Path

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
	to_encode = [m for m in movie_files if any('%s.mkv' % sub in m for sub in diff_list)]

	return to_encode