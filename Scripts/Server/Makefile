all: clean build publish

clean:
	@echo "#\t CLEANING"
	dotnet clean ../../AutoEncode/AutoEncodeServer --configuration Release --nologo --verbosity m

build:
	@echo "###\t BUILDING"
	dotnet build ../../AutoEncode/AutoEncodeServer --configuration Release --os linux --no-self-contained --nologo --verbosity m

publish:
	@echo "#####\t PUBLISHING FILES TO /usr/local/bin/aeserver"
	@sudo mkdir -p /usr/local/bin/aeserver
	sudo dotnet publish ../../AutoEncode/AutoEncodeServer --configuration Release --os linux --output /usr/local/bin/aeserver --no-build --verbosity m
	@sudo mkdir -p /etc/aeserver
	@sudo cp -n ../../Configs/AEServerConfig.yaml /etc/aeserver
	@echo "#######\t MAKE/INSTALL COMPLETE"