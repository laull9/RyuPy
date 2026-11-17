# ryupy: pending
# 源码原始码点：孤立代理、连续代理与真正补充平面字符必须保持区别。
values = ['\ud800x', '\udc00', '\ud83d\ude00', '😀', '\U0001f600', '\ud83d' '\ude00', r'\ud800', '\\ud800', '\ufffd', '\U0000d800']
for value in values:
    print(repr(value), len(value), [ord(item) for item in value])
    print(repr(value[:1]), repr(value[::-1]), [ord(item) for item in value[::2]])
print(values[2] == values[3], values[3] == values[4], values[2] == values[5])
print(values[2] < values[3], values[3] > values[2])
keys = {values[2]: 'points', values[3]: 'emoji', values[0]: 'lone'}
print(len(keys), keys[values[5]], keys[values[4]], keys['\ud800x'])
print(hash(values[2]) == hash(values[5]))
for joined in ['\ud83d' + '\ude00', chr(0xd83d) + chr(0xde00), '\ud83d\ude00' * 2]:
    print(repr(joined), len(joined), [ord(item) for item in joined])
for value in values[:6]:
    try:
        encoded = value.encode('utf-8')
        print('encoded', list(encoded))
    except UnicodeEncodeError:
        print('encode-error')
try:
    ord(values[2])
except TypeError:
    print('two-points-ord-error')
print('unicode-literal-ok')
value = 7
for rendered in [f'\ud800', f'x\ud800{value}', f'{{\ud83d\ude00}}{value}', f'\ud83d' '\ude00']:
    print(repr(rendered), len(rendered), [ord(item) for item in rendered])
def described(argument: '\ud800') -> '\udc00':
    '\ud83d\ude00'
    return argument
print(repr(described.__doc__), len(described.__doc__))
print(repr(described.__annotations__['argument']), repr(described.__annotations__['return']))
for rendered in ['A\ud83d\ude00b'.upper(), 'A\ud83d\ude00B'.lower(), ' \ud83d\ude00 '.strip(), ''.join(['\ud83d', '\ude00'])]:
    print(repr(rendered), len(rendered), [ord(item) for item in rendered])
def same_literal():
    return '\ud83d\ude00'
original = same_literal()
print(original is same_literal(), original[:] is original, original + '' is original, '' + original is original)
print(''.join([original]) is original, original * 1 is original)
for left, right in [('abc', 'abd'), ('中文', '中甲'), ('😀', '\ue000'), ('😀x', '\ud83dy'), ('\ud83d\ude00', '😀')]:
    print(left < right, left == right, left > right)
