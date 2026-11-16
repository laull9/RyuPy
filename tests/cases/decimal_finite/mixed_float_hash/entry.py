# 修正有限float有理数hash的独立合同；不调用Decimal(float)。
from decimal import Decimal
vectors = [('0.5', 0.5), ('0.1', 0.1),
           ('0.1000000000000000055511151231257827021181583404541015625', 0.1),
           ('-0.00', -0.0), ('0', 0.0), ('-1', -1.0),
           ('9007199254740992', 9007199254740992.0),
           ('9007199254740993', 9007199254740992.0)]
for text, floating in vectors:
    value = Decimal(text)
    print(text, repr(floating), value == floating, floating == value,
          value < floating, floating < value, hash(value), hash(floating))
    mapping = {value: 'decimal', floating: 'float'}
    print(len(mapping), mapping[value], mapping[floating], len({value, floating}))
for floating in (0.5, -0.5, 0.1, -0.1, 1.5, 0.0, -0.0, 1.0, -1.0, 1e100, 5e-324):
    print(repr(floating), hash(floating))
# inf/nan不按新增Decimal特殊值实现；仅保护既有float域的结果与键身份。
pos = float('inf'); neg = float('-inf'); nan = float('nan')
print(hash(pos), hash(neg), pos > Decimal('1E+1000000000'), neg < Decimal('-1E+1000000000'))
print(nan == nan, nan != nan, {nan: 7}[nan], nan in {nan})
