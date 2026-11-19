# 一个 dict 布局加普通 mixin：C3 顺序与物理存储是两个独立合同。
class Mixin:
    def label(self):
        return 'mixin'
class DictFirst(dict, Mixin):
    pass
class MixinFirst(Mixin, dict):
    pass
for cls in (DictFirst, MixinFirst):
    value = cls({'a': 1})
    value['b'] = 2
    print(cls.__name__, sorted(value.items()), value.label())
    print([base.__name__ for base in cls.__mro__])
    print(isinstance(value, dict), isinstance(value, Mixin), issubclass(cls, dict))
