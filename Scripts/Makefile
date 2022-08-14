all: clean restore build

clean:
	dotnet clean ../AutomatedFFmpeg/AutomatedFFmpegServer --configuration Release --nologo --verbosity m

restore:
	dotnet restore ../AutomatedFFmpeg/AutomatedFFmpegServer --verbosity q

build:
	dotnet build ../AutomatedFFmpeg/AutomatedFFmpegServer --configuration Release --os linux --no-self-contained --nologo --verbosity m
