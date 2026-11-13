# CP3.12 非数历史及 warning filter/error；生成失败不能提前替换公开类。
from enum import Enum, auto
import warnings
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('always')
    class Text(Enum):
        A = 'text'
        B = auto()
    class Missing(Enum):
        A = None
        B = auto()
    print('nonnumeric', Text.B.value, Missing.B.value)
    print('record', [(w.category.__name__, str(w.message)) for w in seen])
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('ignore', DeprecationWarning)
    class Ignore(Enum):
        A = 10
        B = 1
        C = auto()
    print('ignore', Ignore.C.value, len(seen))
Published = 'old'
with warnings.catch_warnings():
    warnings.simplefilter('error', DeprecationWarning)
    try:
        class Published(Enum):
            A = 10
            B = 1
            C = auto()
            def unreachable(self, x=print('unreachable')):
                pass
    except DeprecationWarning as e:
        print('error', type(e).__name__, str(e))
print('binding', Published)
