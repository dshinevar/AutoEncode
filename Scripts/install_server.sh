#!/bin/bash
# Name: install_server.sh
# Changelog:
#   8/14/2022 - Created
# Description: (LINUX/DEBIAN ONLY) 
#               Builds AutomatedFFmpegServer and places files into appropriate places.
#               Server Files -> /usr/local/bin/afserver
#               Default Empty Config -> /etc/afserver
#               Will need to manually set up a service if wanting to run automatically

echo -e "#\t Building AutomatedFFmpegServer (Makefile)"
make

echo -e "##\t Creating /usr/local/bin/afserver"
sudo mkdir -p /usr/local/bin/afserver

echo -e "###\t Creating /etc/afserver"
sudo mkdir -p /etc/afserver

echo -e "####\t Copying AutomatedFFmpegServer files to /usr/local/bin/afserver"
OUTPUT_FILES_DIR="../AutomatedFFmpeg/AutomatedFFmpegServer/bin/Release/net6.0/linux-x64"
afserver_files=(
    "$OUTPUT_FILES_DIR/AutomatedFFmpegServer" 
    "$OUTPUT_FILES_DIR/AutomatedFFmpegServer.dll" 
    "$OUTPUT_FILES_DIR/AutomatedFFmpegUtilities.dll"
    "$OUTPUT_FILES_DIR/Newtonsoft.Json.dll"
    "$OUTPUT_FILES_DIR/YamlDotNet.dll"
    )

file_missing=false
for file in ${afserver_files[*]}
  do
    if [ ! -f "$file" ]; then
      file_missing=true
      echo "!! Missing expected output file: $file !!"
    fi
  done

if [ "$file_missing" = true ]; then
  echo "!! Output files missing. Will not copy to /usr/local/bin/afserver.!!"
else
  for file in ${afserver_files[*]}
    do
        sudo cp $file /usr/local/bin/afserver
    done
fi