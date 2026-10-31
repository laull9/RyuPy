# ryupy: pending
# M2.1：大整数常量、溢出升级和数值边界。
maximum = 9223372036854775807
minimum = -9223372036854775808
print(maximum + 1, minimum - 1, -minimum, abs(minimum))
print(10**40, (1 << 100) >> 70)
print(minimum // -1, minimum % -1)
print(-(10**30) // 7, -(10**30) % 7)
print(9007199254740993 == 9007199254740992.0)
value = maximum
value += 1
print(value * 3 - 2, int("123456789012345678901234567890"))
