# 通用字符串百分号格式：参数形状、转换时序、错误恢复；不是 ANSI 专用替换。
print('%s|%r|%a' % ('中文', '中文', '中文😀'))
print('%d|%i|%u|%%' % (42, -3, -4))
print('%d|%i|%u' % (True, 3.9, -3.9))
print('%d' % (2 ** 100))
print('%s' % ((1, 2),))
print('%s' % [1, 2])
print('%s' % {})
print('%%' % ())
print('%%' % {})
print('plain' % ())

class Convert:
    def __str__(self):
        print('str')
        return '文本'
    def __repr__(self):
        print('repr')
        return '表示😀'
    def __int__(self):
        print('int')
        return 7
    def __index__(self):
        print('不应调用index')
        return 8

class IndexOnly:
    def __index__(self):
        print('index')
        return 9

value = Convert()
print('%s/%r/%a/%d/%i/%u' % (value, value, value, value, IndexOnly(), value))

for format_text, values in [('%s', ()), ('%s', (1, 2)), ('plain', 1), ('%%', 1), ('%', ()), ('%q', ()), ('%q', 1), ('%d', '3')]:
    try:
        print(format_text % values)
    except (TypeError, ValueError) as error:
        print(type(error).__name__, str(error))

# 后续错误不能提前到第一个 __str__ 之前，也不能重做已有转换。
try:
    print('%s %s' % (value,))
except TypeError:
    print('转换后不足')
try:
    print('%s' % (value, 1))
except TypeError:
    print('转换后过量')

class Broken:
    def __str__(self):
        print('broken')
        raise ValueError('转换失败')

try:
    print('%s %s' % (Broken(), value))
except ValueError as error:
    print(str(error))
print('%s:%d' % ('恢复', 2))
# 数值取模行为不得改变。
print(-7 % 3, 7 % -3, 7.5 % 2.0)

# str 自己处理百分号，右侧对象的 __rmod__ 不能抢先覆盖。
class ReverseModulo:
    def __str__(self):
        return '正向字符串'
    def __int__(self):
        return 9
    def __rmod__(self, other):
        return '错误反射'

print('%s' % ReverseModulo())
text = '%s'
text %= ReverseModulo()
print(text)
for format_text in ['%😀', '😀%q']:
    try:
        print(format_text % 1)
    except ValueError as error:
        print(str(error))
print('%d' % ReverseModulo())
for format_text in ['plain', '%q']:
    try:
        print(format_text % ReverseModulo())
    except (TypeError, ValueError) as error:
        print(type(error).__name__)
