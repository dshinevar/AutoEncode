from getopt import getopt, GetoptError
import os
import subprocess
import sys
import xml.etree.ElementTree as ET

import ffmpeg_script_utils as utils
from ffmpeg_script_utils import usage

def main(argv):
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

	video_file_list = utils.create_video_list(opt, arg)
	selected_video_full_path = utils.user_select_video_file(video_file_list)
	xml_filename = utils.create_video_data_xml(selected_video_full_path)

	xml_file = ET.parse(xml_filename)
	xml_file_root = xml_file.getroot()

	encode_data = utils.build_encode_data(selected_video_full_path, xml_file_root)

	# Program will end if user does not confirm
	utils.confirm_encode(encode_data, xml_filename)

	cmd = utils.build_encode_command(encode_data)
	print('### Running ffmpeg command')
	subprocess.run(cmd, shell=True, stdout=subprocess.PIPE)
	

if __name__ == "__main__":
	main(sys.argv[1:])