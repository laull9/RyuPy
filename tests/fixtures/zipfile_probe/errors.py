import io
import zipfile

class UserError(Exception):
    pass

events = []
class Stream:
    def __init__(self, point, error):
        self.point = point
        self.error = error
    def check(self, point):
        events.append(point)
        if self.point == point:
            raise self.error
    @property
    def read(self):
        self.check('lookup-read')
        return self.do_read
    def do_read(self, size):
        self.check('read')
        return b''
    def seek(self, offset, whence=0):
        self.check('seek')
    def tell(self):
        self.check('tell')
        return 30
    def close(self):
        events.append('close')

for point in ('lookup-read', 'seek', 'tell', 'read'):
    for error in (OSError('io'), zipfile.BadZipFile('bad'), ValueError('value'), UserError('user')):
        events.clear()
        stream = Stream(point, error)
        try:
            print(point, type(error).__name__, 'result', zipfile.is_zipfile(stream), events)
        except (ValueError, UserError) as caught:
            print(point, type(error).__name__, 'raised', caught is error, events)

class ShortReader:
    def seek(self, offset, whence=0):
        pass
    def tell(self):
        return 22
    def read(self, size):
        return b'PK\x05\x06'
print('short-read', zipfile.is_zipfile(ShortReader()))

class OverlongReader(ShortReader):
    def read(self, size):
        return b'PK\x05\x06' + b'\0' * 18 + b'extra'
print('overlong-read', zipfile.is_zipfile(OverlongReader()))
