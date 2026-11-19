# 真实 TypeVar 身份、核心只读项、可写额外项及调用 globals 来源。
import typing as t
T = t.TypeVar('T')
T2 = t.TypeVar('T')
assert type(T) is t.TypeVar and T.__class__ is t.TypeVar
assert isinstance(T, t.TypeVar) and not isinstance(T, type)
assert T is not T2 and T != T2 and T == T
assert hash(T) == hash(T) and len({T, T2}) == 2
keys = {T: 'first', T2: 'second'}
assert keys[T] == 'first' and keys[T2] == 'second'
assert t.get_origin(T) is None and t.get_args(T) == ()
assert T.__module__ == '__main__'
print('identity', True)
class Name(str):
    pass
name = Name('Name')
assert t.TypeVar(name).__name__ is name
assert repr(T) == '~T' and str(T) == '~T'
assert repr(t.TypeVar('C', covariant=True)) == '+C'
assert repr(t.TypeVar('C', contravariant=True)) == '-C'
assert repr(t.TypeVar('C', infer_variance=True)) == 'C'
assert t.TypeVar(name='').__name__ == ''
assert t.TypeVar('a b').__name__ == 'a b'
assert T.__bound__ is None and T.__constraints__ == ()
assert T.__covariant__ is False and T.__contravariant__ is False
assert T.__infer_variance__ is False
print('metadata', True)
for attr in ('__name__', '__bound__', '__constraints__', '__covariant__', '__contravariant__', '__infer_variance__'):
    try:
        setattr(T, attr, None)
    except AttributeError as error:
        print('write', attr, str(error))
    else:
        assert False
    try:
        delattr(T, attr)
    except AttributeError as error:
        print('delete', attr, str(error))
    else:
        assert False
for value in (None, 0, False, [], {}, 'renamed'):
    old_hash = hash(T)
    T.__module__ = value
    assert T.__module__ is value and hash(T) == old_hash
    T.extra = value
    assert T.extra is value
    del T.extra
    assert not hasattr(T, 'extra')
del T.__module__
assert T.__module__ == 'typing'
assert not hasattr(T, '__dict__')
assert not hasattr(T, '__parameters__') and not hasattr(T, '__origin__') and not hasattr(T, '__args__')
print('mutation', True)
# 已存在 frame 的模块名不同于 exec 新 namespace，独立 CP 发现确认保留 __main__。
old_name = __name__
for value in (None, 0, False, [], {}, 'actual_module'):
    __name__ = value
    variable = t.TypeVar('Module')
    assert variable.__module__ == '__main__'
__name__ = old_name
print('caller-module', True)
# 原生不可变类型的 __class__ 不能被实例额外属性遮蔽或更换。
for replacement in (t.TypeVar, int):
    try:
        T.__class__ = replacement
    except TypeError as error:
        print('class-write', str(error))
    else:
        assert False
try:
    del T.__class__
except TypeError as error:
    print('class-delete', str(error))
else:
    assert False
assert T.__class__ is t.TypeVar and type(T) is t.TypeVar
# 函数定义时捕获模块值；当前帧查询实时读取函数可写元数据。
def before_module_change():
    return t.TypeVar('Function').__module__
for value in (None, 0, False, [], {}, 'new_function_module'):
    __name__ = value
    def newly_defined():
        return t.TypeVar('New').__module__
    fresh_lambda = lambda: t.TypeVar('Lambda').__module__
    assert newly_defined.__module__ is value
    assert newly_defined() is value and fresh_lambda() is value
    assert before_module_change() == '__main__'
    assert t.TypeVar('Top').__module__ == '__main__'
    before_module_change.__module__ = value
    assert before_module_change() is value
    before_module_change.__module__ = '__main__'
del before_module_change.__module__
assert before_module_change() is None
__name__ = old_name
print('function-module', True)
