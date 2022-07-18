#!/bin/bash
# Compiles a fresh build of ffmpeg
# Places in home directory
# If all expected files are created, it copies them to /usr/local/bin

get_dependencies () {
  sudo apt-get update -qq && sudo apt-get -y install \
  autoconf \
  automake \
  build-essential \
  cmake \
  git-core \
  libass-dev \
  libfreetype6-dev \
  libgnutls28-dev \
  libtool \
  libxcb1-dev \
  pkg-config \
  texinfo \
  wget \
  yasm \
  zlib1g-dev
}

get_nasm () {
  cd ~/ffmpeg_sources && \
  wget -q https://www.nasm.us/pub/nasm/releasebuilds/2.15.05/nasm-2.15.05.tar.bz2 && \
  tar xjvf nasm-2.15.05.tar.bz2 && \
  cd nasm-2.15.05 && \
  ./autogen.sh && \
  PATH="$HOME/bin:$PATH" ./configure --prefix="$HOME/ffmpeg_build" --bindir="$HOME/bin" && \
  make && \
  make -j4 install
}

get_libx264 () {
  cd ~/ffmpeg_sources && \
  git -C x264 pull 2> /dev/null || git clone --depth 1 https://code.videolan.org/videolan/x264.git && \
  cd x264 && \
  PATH="$HOME/bin:$PATH" PKG_CONFIG_PATH="$HOME/ffmpeg_build/lib/pkgconfig" ./configure --prefix="$HOME/ffmpeg_build" --bindir="$HOME/bin" --enable-static --enable-pic && \
  PATH="$HOME/bin:$PATH" make && \
  make -j4 install
}

get_libx265 () {
  sudo apt-get install libnuma-dev && \
  cd ~/ffmpeg_sources && \
  wget -O x265.tar.bz2 https://bitbucket.org/multicoreware/x265_git/get/master.tar.bz2 && \
  tar xjvf x265.tar.bz2 && \
  cd multicoreware*/build/linux && \
  PATH="$HOME/bin:$PATH" cmake -G "Unix Makefiles" -DCMAKE_INSTALL_PREFIX="$HOME/ffmpeg_build" -DENABLE_SHARED=off -DHIGH_BIT_DEPTH=on -DNATIVE_BUILD=on ../../source  && \
  PATH="$HOME/bin:$PATH" make && \
  make -j4 install
}

get_libmp3lame () {
  cd ~/ffmpeg_sources && \
  wget -q -O lame-3.100.tar.gz https://downloads.sourceforge.net/project/lame/lame/3.100/lame-3.100.tar.gz && \
  tar xzvf lame-3.100.tar.gz && \
  cd lame-3.100 && \
  PATH="$HOME/bin:$PATH" ./configure --prefix="$HOME/ffmpeg_build" --bindir="$HOME/bin" --disable-shared --enable-nasm && \
  PATH="$HOME/bin:$PATH" make && \
  make -j4 install
}

get_libopus () {
  cd ~/ffmpeg_sources && \
  git -C opus pull 2> /dev/null || git clone --depth 1 https://github.com/xiph/opus.git && \
  cd opus && \
  ./autogen.sh && \
  ./configure --prefix="$HOME/ffmpeg_build" --disable-shared && \
  make && \
  make -j4 install
}

build_ffmpeg () {
  cd ~/ffmpeg_sources && \
  wget -q -O ffmpeg-snapshot.tar.bz2 https://ffmpeg.org/releases/ffmpeg-snapshot.tar.bz2 && \
  tar xjvf ffmpeg-snapshot.tar.bz2 && \
  cd ffmpeg && \
  PATH="$HOME/bin:$PATH" PKG_CONFIG_PATH="$HOME/ffmpeg_build/lib/pkgconfig" ./configure \
    --prefix="$HOME/ffmpeg_build" \
    --pkg-config-flags="--static" \
    --extra-cflags="-I$HOME/ffmpeg_build/include -march=native -O2" \
    --extra-ldflags="-L$HOME/ffmpeg_build/lib" \
    --extra-libs="-lpthread -lm" \
    --bindir="$HOME/bin" \
    --enable-gpl \
    --enable-libass \
    --enable-libfreetype \
    --enable-libmp3lame \
    --enable-libopus \
    --enable-libx264 \
    --enable-libx265 \
    --enable-nonfree && \
  PATH="$HOME/bin:$PATH" make && \
  make -j4 install && \
  hash -r
}

LOG=$PWD/ffmpeg_build.log
if [ -f "$LOG" ]; then
  > "$LOG"
else
  touch "$LOG"
fi

rm -rf ~/ffmpeg_build ~/bin/{ffmpeg,ffprobe,ffplay,x264,x265,nasm,ndisasm,lame}

echo -e "#\t Getting dependencies" | tee -a "$LOG"
get_dependencies > /dev/null 2>> "$LOG"

mkdir -p ~/ffmpeg_sources ~/bin

echo -e "##\t Downloading/Building NASM" | tee -a "$LOG"
get_nasm > /dev/null 2>> "$LOG"

echo -e "###\t Downloading/Building libx264" | tee -a "$LOG"
get_libx264 > /dev/null 2>> "$LOG"

echo -e "####\t Downloading/Building libx265 /w HIGH_BIT_DEPTH" | tee -a "$LOG"
get_libx265 > /dev/null 2>> "$LOG"

echo -e "#####\t Downloading/Building libmp3lame" | tee -a "$LOG"
get_libmp3lame > /dev/null 2>> "$LOG"

echo -e "######\t Downloading/Building libopus" | tee -a "$LOG"
get_libopus > /dev/null 2>> "$LOG"

echo -e "#######\t Downloading/Building ffmpeg" | tee -a "$LOG"
build_ffmpeg > /dev/null 2>> "$LOG"

output_files=("$HOME/bin/ffmpeg" "$HOME/bin/ffprobe" "$HOME/bin/lame" "$HOME/bin/x264")
file_missing=false

for file in ${output_files[*]}
  do
    if [ ! -f "$file" ]; then
      file_missing=true
      echo "!! Missing expected output file: $file !!"
    fi
  done

if [ "$file_missing" = true ]; then
  echo "!! Output files missing. Will not copy to /usr/local/bin. Check log file at $LOG !!"
else
  echo -e "######## Copying files to /usr/local/bin"
  sudo cp ~/bin/* /usr/local/bin/
fi
cd $PWD
