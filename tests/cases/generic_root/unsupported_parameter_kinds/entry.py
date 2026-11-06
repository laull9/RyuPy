# CP合法的其他变量种类，G1不能以TypeError伪装尚未支持。
import typing as t
P = t.ParamSpec('P')
Ts = t.TypeVarTuple('Ts')
assert t.Generic[P].__parameters__ == (P,)
assert t.Generic[*Ts].__parameters__ == (Ts,)
print('parameter-kinds', True)
