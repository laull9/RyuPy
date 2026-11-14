# 后续有限Decimal的正合同：无Context算术或float构造。
from decimal import Decimal
for text in ('0', '-0.00', '1.2300', '1E+3', '1E-6', '1E-7',
             '123456789012345678901234567890.123456789', '0E+1000000000',
             '1E+1000000000', '-1E-1000000000'):
    value = Decimal(text)
    print(text, str(value), repr(value), format(value, ''), bool(value))
    print(type(value) is Decimal, isinstance(value, Decimal), Decimal(value) is value)
for text, integer in (('1.00', 1), ('-1', -1), ('-0.00', 0), ('9007199254740993', 9007199254740993)):
    value = Decimal(text)
    print(value == integer, integer == value, hash(value) == hash(integer), value <= integer, integer >= value)
print(Decimal(True), Decimal(False), Decimal(123456789012345678901234567890))
print(Decimal('1E+1000000000') > 10**100, Decimal('1E-1000000000') > 0)
print(Decimal('1E-1000000000') < Decimal('2E-1000000000'))
print(hash(Decimal('1E+1000000000')), hash(Decimal('1E-1000000000')))
print(Decimal('1.00') == Decimal('1'), hash(Decimal('1.00')) == hash(Decimal('1')))
print({Decimal('1.00'): 'first', 1: 'second', True: 'third'}, len({Decimal('1.0'), 1, True}))
