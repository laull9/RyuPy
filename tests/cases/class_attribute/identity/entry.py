# 默认读取保留实际类身份；重复读取经过同一动态属性调用点。
class Plain:
    pass

class Child(Plain):
    pass

class Grandchild(Child):
    pass

def read(obj):
    return obj.__class__

for cls in (Plain, Child, Grandchild):
    obj = cls()
    other = cls()
    for i in range(12):
        assert read(obj) is cls
        assert read(other) is cls
        assert getattr(obj, '__class__') is type(obj)
    assert cls.__class__ is type
    print('default-' + cls.__name__, True)

# 顶层重复类定义尚不支持；完整原合同保留在 duplicate_name_pending。
class Fallback:
    def __getattr__(self, name):
        raise RuntimeError('must not call fallback')
obj = Fallback()
assert read(obj) is Fallback
assert getattr(obj, '__class__', 'missing') is Fallback
assert hasattr(obj, '__class__')
print('before-fallback', True)
assert type.__class__ is type and object.__class__ is type
assert object().__class__ is object
for cls in (int, str, list, dict):
    assert cls.__class__ is type
print('roots', True)

# 类版本变化是合法普通属性写入，不用 __class__ 赋值假装只读布置。
for i in range(12):
    Plain.changed = i
    assert read(Plain()) is Plain
print('version-invalidation', True)
