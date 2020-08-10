from pathlib import Path
import time

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
		msg = f'File {video_full_path} is being written to (file size is changing). May still be ripping. Aborting encoding.' 
	else:
		is_ready = True
		msg = None

	return (is_ready, msg)