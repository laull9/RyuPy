# 只固定已有安全原始键的错误；_type_check先于arity，不能顺带宣称未知合法参数支持。
import typing as t
for root, key in [(t.List, ()), (t.List, (int, str)), (t.Dict, int), (t.Dict, (int, str, bytes)), (t.List, ((int,),)), (t.List, t.Union), (t.List, (t.Generic, int)), (t.Dict, (t.Generic,))]:
    for repeat in range(2):
        try:
            root[key]
        except TypeError as error:
            print(str(error))
        else:
            assert False
print('safe-errors-repeat', True)
