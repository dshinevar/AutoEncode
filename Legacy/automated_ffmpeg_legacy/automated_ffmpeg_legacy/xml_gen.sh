#!/bin/bash

# Generate movie xml with ffprobe

input_movie=$1
output_file=$2

ffprobe -v quiet -read_intervals "%+#2" -print_format xml -show_format -show_streams -show_entries side_data "$1" > "$2"

echo "Created xml file: $2"