# 正常原值、实际旧alias、AttributeError事后同环境fallback及二级None三方控制。
import subject
from typing import List

events = []
missing = object()

class DerivedAttributeError(AttributeError):
    pass

class FalseAgain:
    def __bool__(self):
        events.append('SECOND-BOOL-FORBIDDEN')
        raise RuntimeError('second-bool-forbidden')

class Marker:
    def __init__(self, value, kind):
        self.value = value
        self.kind = kind
    def __bool__(self):
        events.append('module-bool')
        if self.value is missing:
            del subject.__name__
        else:
            subject.__name__ = self.value
        if self.kind == 'runtime':
            raise RuntimeError('original-runtime')
        if self.kind == 'subclass':
            raise DerivedAttributeError('derived-attribute')
        raise AttributeError('original-attribute')

# 保存旧函数后覆盖公开名字，证明reader不能重新查当前绑定。
old = subject.make
subject.make = lambda: 'replacement'
subject.__name__ = 'live-name'
for label, value in (('text', 'reflection'), ('none', None), ('empty', ''),
                     ('integer', 7), ('list', ['raw'])):
    old.__module__ = value
    record = old()
    actual = record.__module__
    expected = value or '__main__'
    print('normal', label, type(actual).__name__, actual is value, actual == expected,
          record(value=3) == {'value': 3})
del old.__module__
print('deleted-module', old().__module__)

again = FalseAgain()
for label, value, kind in (
        ('text', 'after-attribute', 'attribute'), ('empty', '', 'attribute'),
        ('zero', 0, 'attribute'), ('list', [], 'attribute'),
        ('missing', missing, 'attribute'), ('no-second-bool', again, 'attribute'),
        ('subclass', 'after-subclass', 'subclass'),
        ('runtime', 'after-runtime', 'runtime'), ('none-control', None, 'attribute')):
    events.clear()
    subject.__name__ = 'before-attribute'
    old.__module__ = Marker(value, kind)
    try:
        record = old()
        actual = record.__module__
        # None控制必须观察真正CP值；不预写typing或其它推测golden。
        if label == 'none-control':
            print('fallback', label, 'ok', type(actual).__name__, repr(actual), actual is value, events)
        else:
            print('fallback', label, 'ok', actual is value,
                  actual == '__main__' if value is missing else True, events)
    except Exception as error:
        print('fallback', label, 'error', type(error).__name__, str(error), events)

# 非str显示边界不冒称CP等价；直接repr与typing alias必须分别原样记录。
raw = ['display-raw']
old.__module__ = raw
record = old()
print('display-identity', record.__module__ is raw)
for label, value in (('class', record), ('typing-alias', List[record])):
    try:
        print('display', label, 'ok', repr(value))
    except Exception as error:
        print('display', label, 'error', type(error).__name__, str(error))
print('TD done')
