# Requires python plexapi (https://github.com/pkkid/python-plexapi)
# Install: pip3 install plexapi

from plexapi.myplex import MyPlexAccount

class PlexInteractor:
	def __init__(self, username, password, server_name):
		self.username = username
		self.password = password
		self.server_name = server_name

	# Connect to plex server
	# Return: PlexServer Instance
	def __connect(self):
		account = MyPlexAccount(self.username, self.password)
		plex = account.resource(self.server_name).connect()
		return plex

	# Calls plex library update which scans for new files 
	# in that library section (or deleted files)
	def update(self, section):
		plex = self.__connect()
		plex.library.section(section).update()

	# Cleans bundles and empties trash of given section
	def clean(self, section):
		plex = self.__connect()
		plex.library.cleanBundles()
		plex.libary.section(section).emptyTrash()
