all: clean build publish

clean:
	@echo "#\t CLEANING"
	dotnet clean ../AutomatedFFmpeg/AutomatedFFmpegServer --configuration Release --nologo --verbosity m

build:
	@echo "###\t BUILDING"
	dotnet build ../AutomatedFFmpeg/AutomatedFFmpegServer --configuration Release --os linux --no-self-contained --nologo --verbosity m

publish:
	@echo "#####\t PUBLISHING FILES TO /usr/local/bin/afserver"
	@sudo mkdir -p /usr/local/bin/afserver
	sudo dotnet publish ../AutomatedFFmpeg/AutomatedFFmpegServer --configuration Release --os linux --output /usr/local/bin/afserver --no-build --verbosity m
	@sudo mkdir -p /etc/afserver
	@sudo cp -n ../Configs/AFServerConfig.yaml /etc/afserver
	@echo "#######\t MAKE/INSTALL COMPLETE"