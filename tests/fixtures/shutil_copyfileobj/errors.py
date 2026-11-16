import shutil

events = []
class ReadError(Exception):
    pass
class WriteError(Exception):
    pass
read_error = ReadError('read-stop')
write_error = WriteError('write-stop')

class Source:
    def __init__(self, fail):
        self.fail = fail
        self.count = 0
    def read(self, length):
        self.count += 1
        events.append(('read', self.count))
        if self.fail:
            raise read_error
        return b'one'
    def close(self):
        events.append('source-close')

class Sink:
    def write(self, buffer):
        events.append(('write', buffer))
        raise write_error
    def flush(self):
        events.append('flush')
    def close(self):
        events.append('sink-close')

for fail in (True, False):
    events.clear()
    try:
        shutil.copyfileobj(Source(fail), Sink(), 2)
    except (ReadError, WriteError) as error:
        print('caught', error is read_error, error is write_error, events)

class Missing:
    pass
for arguments in ((), (Source(True),), (Source(True), Sink(), 1, 2)):
    events.clear()
    try:
        shutil.copyfileobj(*arguments)
    except TypeError:
        print('arity', len(arguments), events)
events.clear()
try:
    shutil.copyfileobj(Source(True), Sink(), unsupported=1)
except TypeError:
    print('keyword', events)
try:
    shutil.copyfileobj(Missing(), Sink())
except AttributeError:
    print('missing-read', events)
