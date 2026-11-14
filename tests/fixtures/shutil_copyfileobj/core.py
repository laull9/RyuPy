import shutil
from shutil import copyfileobj as saved

events = []
payload = ['original']

class Source:
    def __init__(self):
        self.index = 0

    @property
    def read(self):
        events.append('lookup-read')
        return self.read_impl

    def read_impl(self, length):
        events.append(('read', length))
        self.index += 1
        if self.index == 1:
            return payload
        if self.index == 2:
            return b'x'
        return b''

class Sink:
    def __init__(self):
        self.count = 0

    @property
    def write(self):
        events.append('lookup-write')
        return self.write_impl

    def write_impl(self, buffer):
        self.count += 1
        events.append(('write', self.count, buffer is payload))
        return 0

source = Source()
sink = Sink()
print('identity-result', saved(fsrc=source, fdst=sink, length=3) is None)
print('events', events)

class Truth:
    def __init__(self, answer):
        self.answer = answer
    def __bool__(self):
        events.append(('truth', self.answer))
        return self.answer

class Once:
    def read(self, length):
        events.append(('length', length is requested, length == 19, length == -7))
        return b''

old_size = shutil.COPY_BUFSIZE
shutil.COPY_BUFSIZE = 19
for requested in (0, False, None, -7, Truth(False), Truth(True)):
    events.clear()
    print('length-result', saved(Once(), sink, requested) is None, events)
shutil.COPY_BUFSIZE = old_size

class ChangingSource:
    def __init__(self):
        self.count = 0
    def read(self, length):
        self.count += 1
        events.append(('old-read', self.count))
        if self.count == 1:
            return b'a'
        return b''

class ChangingSink:
    def write(self, buffer):
        events.append('old-write')
        changing.read = new_read
        self.write = new_write
        return 'ignored'

def new_read(length):
    events.append('new-read')
    return b''

def new_write(buffer):
    events.append('new-write')

events.clear()
changing = ChangingSource()
saved(changing, ChangingSink(), 1)
print('cached', events)
