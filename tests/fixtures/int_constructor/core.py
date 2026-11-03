# 固定int参数绑定与公开调用；不依赖完整int子类/元类反射。
log = []
def mark(name, value):
    log.append(name)
    return value
print(int(), int(3.9), int(True), int('10', 16), int('10', base=16))
print(int('12'))
def dynamic_one(value):
    return int(value)
print(dynamic_one('13'))
saved = int
def invoke(fn):
    return fn('ff', 16)
print(saved('101', 2), invoke(saved), int(*['10', 16]), int('10', **{'base': 16}))
for label in ['many', 'unknown', 'duplicate', 'onlybase', 'xkeyword']:
    log.clear()
    try:
        if label == 'many':
            int(mark('first', '10'), mark('second', 16), mark('third', 4))
        elif label == 'unknown':
            int(mark('value', '10'), other=mark('keyword', 16))
        elif label == 'duplicate':
            int(mark('value', '10'), mark('base', 16), base=mark('duplicate', 8))
        elif label == 'onlybase':
            int(base=16)
        else:
            int(x='10')
    except TypeError:
        print(label, log)
class Base:
    def __index__(self):
        log.append('index')
        return 16
log.clear()
print(int(mark('text', '10'), mark('base', Base())), log)
log.clear()
try:
    int(10, Base())
except TypeError:
    print('nontext', log)
class Failure:
    def __index__(self):
        raise RuntimeError('base failed')
try:
    int('not text digits', Failure())
except RuntimeError as error:
    print(str(error))
data = bytearray(b'10')
class ChangingBase:
    def __index__(self):
        data[0] = 50
        return 16
print(int(data, ChangingBase()))
print(int(b'ff', 16), int(bytearray(b'101'), 2), int('１２', 10), int('١٢', 10))
print(int('0b_10', 0), int('00', 0), int('0_0', 0), int('  +12\t', 10))
print(int('f' * 30, 16))
for text, base in [('1__0', 10), ('1_', 10), ('_1', 10), ('010', 0), ('0x10', 10), ('\x1c12', 10), ('Ａ', 36), ('\ud800', 10)]:
    try:
        int(text, base)
    except ValueError:
        print('invalid', base)
class Number:
    def __int__(self):
        log.append('int')
        return 7
    def __index__(self):
        log.append('wrong-index')
        return 8
log.clear()
print(int(Number()), log)
class Indexed:
    def __index__(self):
        return 9
print(int(Indexed()))
def shadow(int):
    return int('x', 16)
def replacement(a, b):
    return a + str(b)
print(shadow(replacement))
