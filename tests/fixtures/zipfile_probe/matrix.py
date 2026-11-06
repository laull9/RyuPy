import io
import zipfile

eocd = b'PK\x05\x06' + b'\0' * 18
record = b'PK\x06\x06' + b'\x2c' + b'\0' * 51
locator = b'PK\x06\x07' + b'\0' * 12 + b'\x01\0\0\0'
comment = eocd[:-2] + b'\x02\0'
for name, data in (
    ('empty', b''),
    ('noise', b'not a zip'),
    ('truncated', eocd[:-1]),
    ('eocd-only', eocd),
    ('prefix', b'P' * 24 + eocd),
    ('short-comment', comment + b'x'),
    ('full-comment', comment + b'xy'),
    ('last-false-signature', comment + b'xxPK\x05\x06'),
    ('window-edge', eocd[:-2] + b'\xff\xff' + b'x' * 65535),
    ('outside-window', eocd[:-2] + b'\xff\xff' + b'x' * 65536),
    ('zip64', record + locator + eocd),
    ('zip64-prefixed', b'SFX-prefix' + record + locator + eocd),
    ('zip64-many-disks', record + locator[:-4] + b'\x02\0\0\0' + eocd),
    ('zip64-bad-offset', record + locator[:8] + b'\xff' * 8 + locator[-4:] + eocd),
    ('zip64-bad-record', b'?' * 56 + locator + eocd),
    ('zip64-bad-size', record[:4] + b'\x2d' + record[5:] + locator + eocd),
):
    stream = io.BytesIO(data)
    stream.seek(1)
    print(name, zipfile.is_zipfile(stream), stream.tell(), stream.closed)
