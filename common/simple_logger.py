from datetime import datetime as dt
from enum import Enum

class Severity(Enum):
	INFO = 1
	ERROR = 2

class SimpleLogger:
	def __init__(self, timezone, log_file):
		self.timezone = timezone
		self.log_file = log_file

	def log(self, severity, msg):
		time_str = dt.now(self.timezone).strftime('%m/%d/%y %H:%M:%S')

		if isinstance(msg, list):
			log_msg = '[%s] - [%s] : %s\n' % (time_str, severity.name, msg[0])
			padding = len(log_msg) - len(msg[0]) - 1
			
			for i in range(1, len(msg)):
				msg[i] = msg[i].rjust(padding + len(msg[i]), ' ') + '\n'
				log_msg += msg[i]
		else:
			log_msg = '[%s] - [%s] : %s\n' % (time_str, severity.name, msg)

		with open(self.log_file, 'a') as f:
			f.write(log_msg)