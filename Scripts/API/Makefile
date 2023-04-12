all: clean build

clean:
	@echo "[#   ] CLEANING"
	dotnet clean ../../AutoEncode/AutoEncodeAPI --configuration Release --nologo --verbosity m

build:
	@echo "[##  ] BUILDING"
	dotnet build ../../AutoEncode/AutoEncodeAPI --configuration Release --os linux --no-self-contained --nologo --verbosity m

publish:
	@echo "[### ] PUBLISHING FILES TO /var/www/aeapi"
	@sudo mkdir -p /var/www/aeapi
	sudo dotnet publish ../../AutoEncode/AutoEncodeAPI --configuration Release --os linux --output /var/www/aeapi --no-build --verbosity m
	@echo "[####] MAKE/INSTALL COMPLETE"