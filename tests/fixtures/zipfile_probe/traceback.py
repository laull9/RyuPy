import zipfile

class ProbeError(Exception):
    pass

class Stream:
    def read(self, size):
        return b''
    def seek(self, offset, whence=0):
        raise ProbeError('zip-user-error')

def probe():
    zipfile.is_zipfile(Stream())

try:
    raise ValueError('outer-context')
except ValueError:
    probe()
