# F1c 新增有界探针：保留外层 __class__ / len / iter 的完整成功与异常轨迹。
import typing

log = []

class TupleMask:
    @property
    def __class__(self):
        log.append('outer.class')
        return tuple

    def __len__(self):
        log.append('outer.len')
        return 2

    def __iter__(self):
        log.append('outer.iter')
        return iter(([int], str))

class OuterClassError:
    @property
    def __class__(self):
        log.append('class.error')
        raise TypeError('outer class marker')

class OuterLengthError(TupleMask):
    def __len__(self):
        log.append('len.error')
        raise TypeError('outer length marker')

class OuterIterationError(TupleMask):
    def __iter__(self):
        log.append('iter.error')
        raise TypeError('outer iteration marker')

class WrongLength(TupleMask):
    def __len__(self):
        log.append('len.one')
        return 1

class ListMask:
    @property
    def __class__(self):
        log.append('args.class')
        return list

    def __iter__(self):
        log.append('args.iter')
        return iter((int,))

    def __len__(self):
        log.append('args.len')
        return 1

for label, parameter in [
    ('tuple-mask', TupleMask()),
    ('class-error', OuterClassError()),
    ('length-error', OuterLengthError()),
    ('iteration-error', OuterIterationError()),
    ('wrong-length', WrongLength()),
    ('list-mask', (ListMask(), str)),
]:
    log.clear()
    try:
        value = typing.Callable[parameter]
    except Exception as error:
        print(label, type(error).__name__, str(error))
    else:
        print(label, repr(value), value.__args__, type(value).__name__)
    print('events', log)

call = typing.Callable[[int], str]
ordinary_type = type(typing.List[int])
call_type = type(call)
print('inheritance', isinstance(call, ordinary_type), issubclass(call_type, ordinary_type))
print('reverse', isinstance(typing.List[int], call_type), issubclass(ordinary_type, call_type))
