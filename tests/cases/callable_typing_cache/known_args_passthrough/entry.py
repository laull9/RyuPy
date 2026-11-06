import typing as t
C=t.Callable
# 参数位只转换；裸Generic/Union/Optional/Literal不是此位的非法类型错误。
for value in (t.Generic,t.Union,t.Optional,t.Literal):
    a=C[[value],int]
    assert a.__args__==(value,int) and a.__parameters__==()
    assert t.get_args(a)==([value],int)
print('known plain forms and Generic root pass through argument conversion: ok')
