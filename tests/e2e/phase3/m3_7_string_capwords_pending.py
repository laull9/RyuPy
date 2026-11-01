# ryupy: pending
# Unicode 首字母标题化不能用 CLR 单字符大写近似；后续字符串协议补齐。
import string
print(string.capwords('  hello\tWORLD\nßETA  '))
print(string.capwords('a::B:', ':'))
