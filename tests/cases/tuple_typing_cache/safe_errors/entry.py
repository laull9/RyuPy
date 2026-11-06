# 只固定安全参数域的真实CP错误，分清普通和尾Ellipsis消息，不扩大未知域。
import typing as t
cases=[
    (((),), 'Tuple[t0, t1, ...]: each t must be a type. Got ().'),
    (((int,),), "Tuple[t0, t1, ...]: each t must be a type. Got (<class 'int'>,)."),
    (((int,),...), "Tuple[t, ...]: t must be a type. Got (<class 'int'>,)."),
    ((t.Union,...), 'Plain typing.Union is not valid as type argument'),
    ((t.Generic,...), "Plain <class 'typing.Generic'> is not valid as type argument"),
]
for raw,message in cases:
    for repeat in range(2):
        try:
            t.Tuple[raw]
        except TypeError as error:
            assert str(error)==message,(str(error),message)
        else:
            assert False
    print(message)
