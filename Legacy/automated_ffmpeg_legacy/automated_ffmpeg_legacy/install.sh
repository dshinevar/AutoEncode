#!/bin/bash

# BASIC INSTALL SCRIPT FOR AUTOMATED_FFMPEG/FFMPEG
# For automated_ffmpeg, it simply copies the necessary files
# to /usr/local/bin

arg=$1
auto_ffmpeg_files=(./automated_ffmpeg/automated_ffmpeg.py ./Common/encode_data.py ./Common/ffmpeg_tools_utilities.py ./Common/list_builders.py ./Common/simple_logger.py ./plex_interactor/plex_interactor.py)

if [[ $arg = "automated_ffmpeg" ]]
then
	echo -e "\n##### Installing automated_ffmpeg #####"
	for file in ${auto_ffmpeg_files[*]}
	do
		cp "$file" /usr/local/bin
		echo "Copied $file to /usr/local/bin"
	done
	echo "##### Completed installing automated_ffmpeg #####"
	echo -e "Make sure to run this command: systemctl daemon-reload\n"
elif [[ $arg = "ffmpeg" ]]
then
	echo "Installing ffmpeg"
	sh ./ffmpeg_build/ffmpeg_build.sh
else
	echo "Usage: bash install.sh [automated_ffmpeg | ffmpeg]"
fi