# 独立已知CP差异：两个类定义分别执行，不让第一个不支持阻断第二个。
# 只捕获预期类别与精确错误信息；其它异常、不同错误信息均失败，不是兼容通过。
def accumulate(initial, values):
    total = initial
    for i in range(len(values)):
        total += values[i]
    return total

shared = []
try:
    class Reflected(str):
        def __radd__(self, other):
            return shared
except NotImplementedError as error:
    if type(error) is not NotImplementedError or str(error) != 'str subclass slot __radd__ is not supported':
        raise
    print('radd known-unsupported', str(error))
else:
    print('radd supported', accumulate('prefix', ['a', Reflected('rhs')]) is shared)

try:
    class InPlace(str):
        def __iadd__(self, other):
            return shared
except NotImplementedError as error:
    if type(error) is not NotImplementedError or str(error) != 'str subclass slot __iadd__ is not supported':
        raise
    print('iadd known-unsupported', str(error))
else:
    print('iadd supported', accumulate(InPlace('seed'), ['suffix']) is shared)
