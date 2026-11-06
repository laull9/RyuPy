# getter 次数及异常传播；不把一般 __getattr__ 回退协议算进本切。
events = []
class Broken:
    @property
    def __class__(self):
        events.append('get')
        raise ValueError('class getter')
obj = Broken()
assert type(obj) is Broken and Broken.__class__ is type
assert len(events) == 0
for i in range(8):
    try:
        obj.__class__
    except ValueError as error:
        assert str(error) == 'class getter'
    else:
        raise AssertionError('getter did not raise')
assert len(events) == 8
print('value-error', len(events))
class Missing:
    @property
    def __class__(self):
        events.append('missing')
        raise AttributeError('missing getter')
obj = Missing()
assert getattr(obj, '__class__', 'default') == 'default'
assert not hasattr(obj, '__class__')
assert len(events) == 10
print('attribute-error', len(events))
class NoGetter:
    __class__ = property()
obj = NoGetter()
assert type(obj) is NoGetter and NoGetter.__class__ is type
try:
    obj.__class__
except AttributeError:
    print('no-getter', True)
else:
    raise AssertionError('missing getter silently returned class')
