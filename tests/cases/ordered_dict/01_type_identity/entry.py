# 精确类型、原生祖先与协议身份，不把 OrderedDict 当 dict 别名。
from collections import OrderedDict as OD
from collections import OrderedDict
from collections.abc import Mapping, MutableMapping

d = OD()
print('type', OD is OrderedDict, OD is not dict, type(d) is OD, type(d) is not dict)
print('name', OD.__name__, OD.__qualname__, OD.__module__, repr(OD))
print('ancestry', OD.__bases__, OD.__mro__)
print('subclass', issubclass(OD, dict), issubclass(OD, object), issubclass(OD, Mapping), issubclass(OD, MutableMapping))
print('instance', isinstance(d, dict), isinstance(d, Mapping), isinstance(d, MutableMapping))
print('empty', len(d), bool(d), repr(d), list(d))
assert type(d) is OD and OD is not dict and isinstance(d, dict)
try:
    hash(d)
except TypeError as exc:
    print('hash', type(exc).__name__, str(exc), exc.args)
else:
    raise AssertionError('OrderedDict 必须不可 hash')
print('DONE type_identity')
