# 二进制decode四入口共享参数绑定，不能误触上游Python2兼容分支。
for owner in (bytes, bytearray):
    value = owner(b'caf\xc3\xa9')
    saved = value.decode
    root = owner.decode
    print(value.decode(encoding='utf-8'))
    print(saved(errors='strict', encoding='utf_8'))
    print(root(value, 'utf-8', 'strict'))
    print(owner.decode(value, encoding='utf-8', errors='strict'))
    for f in (saved,):
        for args, kwargs in ((('utf-8',), {'encoding': 'ascii'}), ((), {'encoding': None}), ((), {'errors': 1}), ((), {'extra': 1}), (('utf8', 'strict', 1), {})):
            try:
                f(*args, **kwargs)
            except TypeError:
                print('TypeError')
    try:
        owner(b'\xff').decode(encoding='ascii')
    except UnicodeDecodeError:
        print('strict-ascii')
    try:
        owner(b'\xff').decode(encoding='utf-8')
    except UnicodeDecodeError:
        print('strict-utf8')
    print(owner(b'\xff').decode(encoding='latin-1'))
    try:
        value.decode(encoding='unknown-codec')
    except LookupError:
        print('LookupError')
for owner in (bytes, bytearray):
    print(owner(b'\xff\xfeA\x00=\xd8\x00\xde').decode(encoding='utf-16'))
    print(owner(b'\xfe\xff\x00A\xd8=\xde\x00').decode(encoding='utf-16'))
    print(owner(b'\x00A').decode(encoding='utf-16-be'))
    print(owner(b'A\x00').decode(encoding='utf-16-le'))
    print(owner(b'').decode(encoding='utf-16'))
    for data in (b'\xff', b'\xff\xfeA', b'\xff\xfe\x00\xd8', b'\xfe\xff\xdc\x00'):
        try:
            owner(data).decode('utf-16')
        except UnicodeDecodeError:
            print('strict-utf16')
