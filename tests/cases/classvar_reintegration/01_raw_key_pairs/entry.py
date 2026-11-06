# ryupy: pending
# 原R1另源保留；本源区分原始None键、等价alias键与真正的新tuple键。
import typing as t
CV=t.ClassVar
key=t.List[int]
equal_key=t.List[(int,)]
assert key == equal_key and key is not equal_key
warm=t.List[key]
dict_none=t.Dict[None,key]
tuple_scalar=t.Tuple[key]
callable_none=t.Callable[[None],None]
callable_type=t.Callable[[type(None)],type(None)]
assert callable_none == callable_type and callable_none is not callable_type
assert callable_none.__args__ == (type(None),type(None))
try:
    t.ClassVar=list
    assert t.List[key] is warm and t.List[equal_key] is warm
    assert t.Dict[None,equal_key] is dict_none
    assert t.Tuple[equal_key] is tuple_scalar
    for label,root,raw in (
        ('list-new-tuple',t.List,(key,)),
        ('dict-none-type',t.Dict,(type(None),key)),
        ('tuple-new-tuple',t.Tuple,(key,)),
    ):
        try:
            root[raw]
        except TypeError as error:
            assert str(error)=='typing.List[int] is not valid as type argument'
            print(label,str(error))
        else:
            assert False,label
    # 参数是int，不是产生的List alias；故新raw miss成功而不合并归一化身份。
    integer=t.List[int]
    assert t.List[(int,)] is equal_key and equal_key is not integer
finally:
    t.ClassVar=CV
recovered=t.List[(key,)]
assert recovered == warm and recovered is not warm
assert t.List[(key,)] is recovered
recovered_dict=t.Dict[type(None),key]
assert recovered_dict == dict_none and recovered_dict is not dict_none
assert t.Dict[type(None),equal_key] is recovered_dict
recovered_tuple=t.Tuple[(key,)]
assert recovered_tuple == tuple_scalar and recovered_tuple is not tuple_scalar
print('raw-keys-none-equivalence-recovery',True)
