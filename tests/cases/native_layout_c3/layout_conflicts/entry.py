# 不同 native 布局或不一致 C3 必须在类体前拒绝，不能发布半成品。
old = object()
Target = old
events = []
try:
    class Target(dict, list):
        events.append('dict-list-body')
except TypeError:
    print('dict-list conflict', Target is old, events)
try:
    class Target(dict, str):
        events.append('dict-str-body')
except TypeError:
    print('dict-str conflict', Target is old, events)
class Mixin:
    pass
class DictFirst(dict, Mixin):
    pass
class MixinFirst(Mixin, dict):
    pass
try:
    class Target(DictFirst, MixinFirst):
        events.append('c3-body')
except TypeError:
    print('c3 conflict', Target is old, events)
print(sorted(DictFirst({'still': 1}).items()), sorted(MixinFirst({'valid': 2}).items()))
