# ryupy: pending
# 显式g/G有界差分：共297次format调用；不输出原float的repr或增加新API依赖。
values = (
    0.0, -0.0, 1.0, -1.0, 1.23456789,
    1.25, 1.75, 2.5, 3.5, -1.25,
    9.999995, 999999.5, 1e6, 1e-4, 1e-5,
    9.999999e-5, 9.999999e5, 1.7976931348623157e308,
    2.2250738585072014e-308, 2.225073858507201e-308, 5e-324,
    float('inf'), float('-inf'), float('nan'), float('-nan'),
)
specs = ('g', 'G', '.0g', '.1g', '.2g', '.6g', '.17g', '.18g', '.50g', '.100g')
assert len(values) == 25
assert len(specs) == 10
# 25*10=250次：ties、rounded exponent阈值、精确尾部和特殊数。
for index, value in enumerate(values):
    for spec in specs:
        print('core', index, spec, format(value, spec))

padding_values = (-0.0, 1.25, float('inf'), float('nan'))
padding_specs = ('+010g', ' 010G', '<12g', '>12g', '^13G', '*=+12g', '*<012g', '😀^12g')
# 4*8=32次：显式fill优先、符号内零填充、左右居中和Unicode单码点fill。
for index, value in enumerate(padding_values):
    for spec in padding_specs:
        print('padding', index, spec, format(value, spec))

# 4*2=8次：缺省g精度明确为6，不是往返最短repr。
for value in (1.23456789, 999999.5, 1e-5, -0.0):
    assert format(value, 'g') == format(value, '.6g')

# 3次：CP自身precision上界guard，不请求巨大格式化分配。
for index, value in enumerate((1.0, float('inf'), float('nan'))):
    try:
        format(value, '.2147483648g')
    except ValueError as error:
        assert str(error) == 'precision too big'
        print('precision', index, type(error).__name__, str(error))
    else:
        raise AssertionError('oversized precision accepted')

# 4次：非法type在finite/special上都必须先失败，不以NI替代ValueError。
for index, value in enumerate((1.0, float('nan'))):
    for spec in ('s', 'x'):
        try:
            format(value, spec)
        except ValueError as error:
            print('invalid', index, spec, type(error).__name__, str(error))
        else:
            raise AssertionError('invalid float type accepted')
print('format-general-float-ok')
