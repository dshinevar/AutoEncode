from datetime import datetime as dt
from enum import Enum
import os
from pathlib import Path

class Severity(Enum):
	INFO = 1
	ERROR = 2
	FATAL = 3

# Class: SimpleLogger
# Description: A simple logger (hence the name).
# Requires a pytz timezone and a log_file name/path passed
# in. Can log a message string or a list of strings.
# Uses the Severity Enum
class SimpleLogger:
	def __init__(self, timezone, log_file):
		self.timezone = timezone
		self.log_file = log_file

	def log(self, severity, msg):
		time_str = dt.now(self.timezone).strftime('%m/%d/%y %H:%M:%S')

		if isinstance(msg, list):
			log_msg = f'[{time_str}] - [{severity.name}] : {msg[0]}\n'
			padding = len(log_msg) - len(msg[0]) - 1
			
			for i in range(1, len(msg)):
				msg[i] = msg[i].rjust(padding + len(msg[i]), ' ') + '\n'
				log_msg += msg[i]
		else:
			log_msg = f'[{time_str}] - [{severity.name}] : {msg}\n'

		with open(self.log_file, 'a') as f:
			f.write(log_msg)

# Class: SimpleLoggerWithRollover
# Description: Inherits from SimpleLogger and adds a manual rollover function.
# If max_bytes is left as default (-1), it will act the same as SimpleLogger 
# even if check_rollover is called.  Setting max_bytes but leaving backup_count
# at 0 will cause the original log file to just be deleted.
# In order for rollover function to run, check_rollover must be called manually.
# This was designed with wanting to rollover only at a specific point in code.
# Because of this, "max_bytes" is only somewhat relative. Potentially, the log file
# could go over max_bytes before being rolled over depending on when check_rollover 
# is called.
class SimpleLoggerWithRollover(SimpleLogger):
	def __init__(self, timezone, log_file, max_bytes=-1, backup_count=0):
		super().__init__(timezone, log_file)
		self.max_bytes = max_bytes
		self.backup_count = backup_count

	def __do_rollover(self):
		if self.backup_count > 0:
			for i in range(self.backup_count, 0, -1):
				file = f'{self.log_file}.{i}'
				if os.path.exists(file):
					if i == self.backup_count:
						os.remove(file)
					else:
						os.rename(file, f'{self.log_file}.{i + 1}')

			os.rename(self.log_file, f'{self.log_file}.1')
			with open(self.log_file, 'w'):
				pass
		else:
			with open(self.log_file, 'w'):
				pass

	def check_rollover(self):
		if self.max_bytes >= -1:
			current_log_file_size = Path(self.log_file).stat().st_size

			if current_log_file_size >= self.max_bytes:
				self.__do_rollover()


