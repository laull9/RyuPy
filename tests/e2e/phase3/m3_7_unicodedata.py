import unicodedata as u

print(u.unidata_version)
for cp in (0, 65, 0xA1, 0x300, 0x315, 0x345, 0x5B0, 0x93C, 0x4E00, 0xFF01, 0xFF66, 0x1F600, 0x1D165, 0x378, 0xD800, 0xDFFF, 0x20000, 0x2FFFD, 0x2FFFE, 0x30000, 0x3FFFD, 0x10FFFF):
    c = chr(cp)
    print(cp, u.east_asian_width(c), u.combining(c))
for bad in ('', 'ab', 'e\u0301', 1, None, b'a'):
    try:
        u.east_asian_width(bad)
    except TypeError:
        print('width type error')
    try:
        u.combining(bad)
    except TypeError:
        print('combining type error')
try:
    u.east_asian_width(chr='a')
except TypeError:
    print('positional only')
try:
    u.combining()
except TypeError:
    print('missing argument')
try:
    u.combining('a', 'b')
except TypeError:
    print('extra argument')
