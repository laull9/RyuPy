# 已冻结空 slots 的后续元数据重绑不能伪造新的物理布局冲突。
class M:
    __slots__ = ()
M.__slots__ = ('metadata_only',)
try:
    class D(dict, M): pass
    x = D(a=1)
    print('ok', list(x), hasattr(x, 'metadata_only'))
except Exception as e:
    print(type(e).__name__, str(e))

# 原本没有 slots 的普通类也已冻结；晚加同名元数据不应重新解析为物理槽。
class Unslotted:
    pass
Unslotted.__slots__ = ('metadata_only',)
try:
    class WithDictionary(dict, Unslotted):
        pass
    value = WithDictionary(a=1)
    print('late metadata', list(value), hasattr(value, 'metadata_only'))
    value.metadata_only = 'attribute'
    value['metadata_only'] = 'mapping'
    print(value.metadata_only, value['metadata_only'])
except Exception as exc:
    print(type(exc).__name__, str(exc))
