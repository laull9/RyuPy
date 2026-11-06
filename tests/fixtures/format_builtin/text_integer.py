# ryupy: pending
# 阶段2真实支持子集；合法但未支持的数值格式另列边界，不伪造CP golden。
for value in ('', 'abc', 'a🙂界', 'é'):
    for spec in ('', 's', '<7', '>7', '^8', '*^8s', '🙂^7s', '.2s', '>7.2s', '05', '>05', '<05', '５s'):
        print(repr(value), repr(spec), repr(format(value, spec)))
for value in (0, 1, -1, 42, -42, 9223372036854775808, -123456789012345678901234567890):
    for spec in ('', 'd', 'b', 'o', 'x', 'X', '+8d', ' 8d', '#x', '#08x', '=+9d', '*>9d', '*<09d', '*=+10x', '^12X', '０８d'):
        print(value, repr(spec), repr(format(value, spec)))
for value in (False, True):
    for spec in ('', 'd', '5', '>5', '05', '#06x'):
        print(value, repr(spec), repr(format(value, spec)))
for value, spec in (('abc', '+s'), ('abc', '=5s'), ('abc', '#s'), ('abc', ',s'), ('abc', 'd'), (7, '.2d'), (7, 'q'), (True, 's'), (1.5, 'x')):
    try:
        format(value, spec)
    except ValueError as error:
        print(repr(value), repr(spec), type(error).__name__, str(error))
    else:
        raise AssertionError('invalid spec accepted')
for value in (0.0, -0.0, 1.5, float('inf'), float('nan')):
    print(format(value))
print('format-text-integer-ok')

# 默认十进制位数限制：有限几KB边界，不调整全局配置或采样性能。
limit_ok = 10 ** 4299
assert len(format(limit_ok, 'd')) == 4300
limit_exceeded = 10 ** 4300
try:
    format(limit_exceeded, 'd')
except ValueError as error:
    print('decimal-limit', type(error).__name__, str(error))
else:
    raise AssertionError('decimal digit limit missing')
hex_result = format(limit_exceeded, 'x')
assert len(hex_result) == 3572
assert int(hex_result, 16) == limit_exceeded
print('format-decimal-limit-ok')
