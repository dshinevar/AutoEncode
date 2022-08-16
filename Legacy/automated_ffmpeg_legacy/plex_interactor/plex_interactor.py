# Requires python plexapi (https://github.com/pkkid/python-plexapi)
# Install: pip3 install plexapi

from plexapi.server import PlexServer

class PlexInteractor:
	def __init__(self, baseurl, token):
		self.baseurl = baseurl
		self.token = token

	# Connect to plex server
	# Return: PlexServer Instance
	def __get_server(self):
		plex = PlexServer(self.baseurl, self.token)
		return plex

	# Calls plex library update which scans for new files 
	# in that library section (or deleted files)
	def update(self, section):
		plex = self.__get_server()
		plex.library.section(section).update()

	# Cleans bundles and empties trash of given section
	def clean(self, section):
		plex = self.__get_server()
		plex.library.cleanBundles()
		plex.libary.section(section).emptyTrash()
