import io
import shutil

source = io.BytesIO(b'abcdef')
sink = io.BytesIO(b'.....')
source.seek(2)
sink.seek(1)
print('binary-result', shutil.copyfileobj(source, sink, 2) is None)
print('binary', source.tell(), sink.tell(), sink.getvalue(), source.closed, sink.closed)
source = io.StringIO('A😀BC')
sink = io.StringIO('.....')
source.seek(1)
sink.seek(2)
shutil.copyfileobj(source, sink, -1)
print('text', source.tell(), sink.tell(), sink.getvalue(), source.closed, sink.closed)
with open('source.bin', 'wb') as stream:
    stream.write(b'012345')
with open('source.bin', 'rb') as source:
    with open('target.bin', 'w+b') as sink:
        source.seek(1)
        shutil.copyfileobj(source, sink, 2)
        print('file', source.tell(), sink.tell(), source.closed, sink.closed)
        sink.seek(0)
        print('file-data', sink.read())
