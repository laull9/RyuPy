import io
import zipfile

data = b'P' * 24 + b'PK\x05\x06' + b'\0' * 18
with open('archive.bin', 'wb') as stream:
    stream.write(data)
print('path', zipfile.is_zipfile('archive.bin'))
print('missing', zipfile.is_zipfile('missing.bin'))
stream = io.BytesIO(data)
stream.seek(3)
print('bytesio', zipfile.is_zipfile(stream), stream.tell(), stream.closed)

events = []
class Stream:
    def __init__(self):
        self.inner = io.BytesIO(data)
        self.phase = 0
    @property
    def read(self):
        events.append(('lookup-read', self.phase))
        if self.phase == 0:
            return self.first_read
        return self.later_read
    def first_read(self, size):
        events.append(('first-read', size))
        self.phase = 1
        return self.inner.read(size)
    def later_read(self, size):
        events.append(('later-read', size))
        return self.inner.read(size)
    @property
    def seek(self):
        events.append('lookup-seek')
        return self.do_seek
    def do_seek(self, offset, whence=0):
        events.append(('seek', offset, whence))
        return self.inner.seek(offset, whence)
    @property
    def tell(self):
        events.append('lookup-tell')
        return self.do_tell
    def do_tell(self):
        events.append('tell')
        return self.inner.tell()
    def close(self):
        events.append('close')

stream = Stream()
print('custom', zipfile.is_zipfile(stream), stream.inner.tell())
print('events', events)
