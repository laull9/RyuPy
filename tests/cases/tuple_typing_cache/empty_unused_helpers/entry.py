# 独立进程首个空Tuple真正cold不读取类型检查或Ellipsis全局。
import typing as t
# 本进程此前未创建 Tuple[()]；这些 helper 不在空参数真正冷路径中消费。
for helper in ['_type_check','_type_convert','_check_generic','_TypingEllipsis','Generic','Protocol','ClassVar','Final','Any','_TupleType']:
    setattr(t,helper,None)
a=t.Tuple[()]
assert a.__args__ == () and a.__parameters__ == ()
assert a.__origin__ is tuple
print('empty-cold-does-not-consume-check-or-ellipsis',True)
