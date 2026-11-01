# 大但精确可表示的整数/浮点必须共享哈希；字典和集合不能保留两个相等键。
for integer in [(1 << 63) - 1024, -(1 << 63), 1 << 63, -(1 << 64), 1 << 100]:
    floating = float(integer)
    print(integer == floating, hash(integer) == hash(floating))
    mapping = {integer: 'integer'}
    mapping[floating] = 'float'
    print(len(mapping), mapping[integer], len({integer, floating}))
print((1 << 100) + 1 == float(1 << 100))
