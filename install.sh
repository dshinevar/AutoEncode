#!/bin/bash

# BASIC INSTALL SCRIPT FOR AUTOMATED_FFMPEG/FFMPEG
# For automated_ffmpeg, it simply copies the necessary files
# to /usr/local/bin

arg=$1
auto_ffmpeg_files=(./automated_ffmpeg/automated_ffmpeg.py ./common/encode_data.py ./common/ffmpeg_tools_utilities.py ./common/list_builders.py ./common/simple_logger.py ./plex_interactor/plex_interactor.py)

if [[ $arg = "automated_ffmpeg" ]]
then
	echo -e "\n##### Installing automated_ffmpeg #####"
	for file in ${auto_ffmpeg_files[*]}
	do
		cp "$file" /usr/local/bin
		echo "Copied $file to /usr/local/bin"
	done
	echo "##### Completed installing automated_ffmpeg #####"
	echo -e "Remember to reload systemctl daemon\n"
elif [[ $arg = "ffmpeg" ]]
then
	echo "Installing ffmpeg"
else
	echo "Usage: bash install.sh [automated_ffmpeg | ffmpeg]"
fi