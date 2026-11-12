# 合法十进制 Unicode 数字、宽松下划线和外围空白；不混入非法构造异常域。
from decimal import Decimal
vectors = [
    ('ascii_spacing', ' \t+0012.3400\r\n'),
    ('arabic', '١٢٣.٤٥'),
    ('devanagari', '१२.३४'),
    ('mixed_digits', '-１٢.३۴'),
    ('supplementary_digits', '𝟙𝟚.𝟛𝟜'),
    ('unicode_exponent', '١٢.٣٠e-٢'),
    ('unicode_space', '\u00a0\u2003-０.００\u202f\u3000'),
    # CPython 还把这四个 ASCII 分隔控制字符视为合法外围空白。
    ('ascii_file_separator', '\x1c1.2300\x1c'),
    ('ascii_group_separator', '\x1d-0.00\x1d'),
    ('ascii_record_separator', '\x1e+0.5\x1e'),
    ('ascii_unit_separator', '\x1f1E+2\x1f'),
    ('loose_underscores', '_+_0_0_1_2_._3_0_0_E_-_2_'),
    ('underscore_fraction', '_.5_'),
    ('underscore_point', '1_.__'),
    ('underscore_exponent', '1e_+_2__'),
    ('outer_spacing', ' _1_ '),
]
for label, text in vectors:
    value = Decimal(text)
    print(label, str(value), repr(value), format(value, ''), bool(value), hash(value))
    print(type(value) is Decimal, isinstance(value, Decimal), Decimal(value) is value)
