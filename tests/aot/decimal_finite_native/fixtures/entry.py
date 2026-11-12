# 有限精确Decimal的真实Native图；不调用Context、浮点构造或未支持算术。
from decimal import Decimal
from values import SMALLEST_SUBNORMAL

for source in ['12.3400', '-0.000', '123456789012345678901234567890123456789',
               '1E+1000000000', '1E-1000000000']:
    value = Decimal(source)
    print(str(value), repr(value), format(value, ''), bool(value), hash(value))
for original in [0, 1, -1, 123456789012345678901234567890123456789, True]:
    value = Decimal(original)
    print(str(value), value == original, original == value,
          hash(value) == hash(original), len({value: 'decimal', original: 'number'}))
copy = Decimal('2.00')
print(Decimal(copy) is copy, str(Decimal(value='2.00')))
for text, number in [('0.5', 0.5), ('0.1', 0.1),
                     ('0.1000000000000000055511151231257827021181583404541015625', 0.1),
                     ('-0.000', -0.0), (SMALLEST_SUBNORMAL, 5e-324)]:
    value = Decimal(text)
    print(value == number, number == value, value < number, number < value,
          hash(value), hash(number))
    forward = {value: 'decimal', number: 'number'}
    reverse = {number: 'number', value: 'decimal'}
    print(len(forward), forward[value], forward[number],
          len(reverse), reverse[value], reverse[number])
print(Decimal('1E+1000000000') > 1e300, Decimal('1E-1000000000') < 5e-324)
