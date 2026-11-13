# join精确list快路：文本值/单项身份/输入不变；其它iterator仍先耗尽再校验。
single = 'one' + str(123)
for parts in ([], [single], ['', single, ''], ['中文', '😀', 'é']):
    original = parts[:]
    for separator in ('', '|', '😀'):
        value = separator.join(parts)
        print(value, len(value), parts == original)
        if len(parts) == 1:
            print(value is single)
events = []
def bad():
    events.append('first')
    yield 1
    events.append('second')
    raise ValueError('later')
try:
    ''.join(bad())
except ValueError:
    print(events)
class Text(str):
    def __str__(self):
        raise ValueError('must not stringify')
print('|'.join([Text('a'), Text('😀')]))
class Parts(list):
    pass
print(','.join(Parts(['base', 'list'])))
try:
    ''.join(['a', 1, 'b'])
except TypeError:
    print('TypeError')
