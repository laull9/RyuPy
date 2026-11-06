import shutil

class CopyError(Exception):
    pass

class Source:
    def read(self, length):
        return b'chunk'

class Sink:
    def write(self, buffer):
        raise CopyError('copy-user-error')

def transfer():
    shutil.copyfileobj(Source(), Sink(), 4)

try:
    raise ValueError('outer-context')
except ValueError:
    transfer()
