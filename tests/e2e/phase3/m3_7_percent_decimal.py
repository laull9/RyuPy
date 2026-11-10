# ryupy: pending
# 十进制百分号 ASCII flags/字面宽度的唯一同源正面入口；无参数执行完整合同。
# 只用已存在的循环、类协议、tuple/dict和异常语法；NI/宿主容量另图，不混CP黄金。
flags = [
    '', '#', '0', '#0', '-', '#-', '0-', '#0-',
    '+', '#+', '0+', '#0+', '-+', '#-+', '0-+', '#0-+',
    ' ', '# ', '0 ', '#0 ', '- ', '#- ', '0- ', '#0- ',
    '+ ', '#+ ', '0+ ', '#0+ ', '-+ ', '#-+ ', '0-+ ', '#0-+ ',
]
rows = 0
for conversion in ['d', 'i', 'u']:
    for flag in flags:
        for width in ['', '1', '6', '06']:
            for value in [0, 12, -12]:
                specification = '%' + flag + width + conversion
                print('flags', repr(specification), value, repr(specification % value))
                rows += 1
print('flags-count', rows)

# 重复/乱序flag、宽度不足、长前导零以及1024宽度不引入巨量分配。
for specification in ['%00++##--  6d', '% +06i', '%0-+ 6u', '%0000000000000000000000004d', '%1i']:
    for value in [0, 12, -12]:
        print('repeat', repr(specification), value, repr(specification % value))
print('wide', repr('%1024d' % 1))
for specification, value in [
    ('%+04d', True), ('%+04i', False), ('%04u', -3.9), ('%+04d', 3.9),
    ('%+04i', -0.0), ('%021d', -9223372036854775808),
    ('%+040u', 2 ** 100), ('%+040d', -(2 ** 100)),
]:
    print('number', specification, repr(specification % value))
print('mixed', '😀%04d中%%/%s/%r/%a' % (-12, 'é', '中', '😀'))
print('tuple', '%04d/%+06i/%-6u' % (12, -12, 0))
print('dict', '%(a)04d/%(b)+06i/%(a)-6u' % {'a': 12, 'b': -12})

events = []


class Number:
    def __int__(self):
        events.append('int')
        return -12

    def __index__(self):
        events.append('wrong-index')
        return 99


class IndexOnly:
    def __index__(self):
        events.append('index')
        return 8


class BadInt:
    def __int__(self):
        events.append('bad-int')
        return '12'

    def __index__(self):
        events.append('wrong-fallback')
        return 99


class TypeInt:
    def __int__(self):
        events.append('type-int')
        raise TypeError('original-type')


class ValueInt:
    def __int__(self):
        events.append('value-int')
        raise ValueError('original-value')


for value in [Number(), IndexOnly(), BadInt(), TypeInt(), ValueInt()]:
    events.clear()
    try:
        print('protocol', repr('%+06d' % value))
    except (TypeError, ValueError) as error:
        print('protocol-error', type(error).__name__, str(error))
    print('events', events)

# 先前转换不会被后续参数错误回滚、预验证或重复执行。
for specification, values in [
    ('%04d/%04d', (Number(),)), ('%04d', (Number(), 1)),
    ('%04d/%', (Number(),)), ('%04d/%4q', (Number(), 1)),
]:
    events.clear()
    try:
        print(specification % values)
        raise AssertionError('expected argument/specification failure')
    except (TypeError, ValueError) as error:
        print('later-error', repr(specification), type(error).__name__, str(error), events)

state = {'later': 1}


class MutatingNumber:
    def __int__(self):
        events.append('mutate')
        state['later'] = 9
        return 12


class Mapping:
    def __getitem__(self, key):
        events.append(key)
        if key == 'missing':
            raise KeyError(key)
        if key == 'first':
            return MutatingNumber()
        return state['later']

    def __str__(self):
        events.append('mapping-str')
        return 'mapping'


mapping = Mapping()
events.clear()
print('mapping', '%(first)04d/%(later)+06i/%(later)-6u' % mapping)
print('events', events)
events.clear()
print('mapping-mixed', '%s/%((nested))04d/%()04u' % mapping)
print('events', events)
for specification in ['%(missing)9223372036854775808d', '%(first)9223372036854775808d', '%(first)04', '%(unfinished']:
    events.clear()
    try:
        print(specification % mapping)
        raise AssertionError('expected mapping/specification failure')
    except (TypeError, ValueError, KeyError) as error:
        print('mapping-error', repr(specification), type(error).__name__, str(error), events)

# huge width仅与空tuple配对：没有可转换值，CP不分配巨大输出。
for specification in [
    '%6', '%+', '%04', '%4q', '%4-d', '%4+d', '%4 d', '%4#d', '%0%',
    '%٤d', '%０4d', '😀%4q', '%4😀',
    '%2147483648d', '%9223372036854775807d', '%9223372036854775808d',
]:
    try:
        print(specification % ())
        raise AssertionError('expected empty-tuple failure')
    except (TypeError, ValueError) as error:
        print('empty-error', repr(specification), type(error).__name__, str(error))
for specification in ['%6', '%+', '%04', '%4q', '%4-d', '%4+d', '%4 d', '%4#d', '%0%', '%٤d', '%０4d', '😀%4q', '%4😀']:
    try:
        print(specification % 1)
        raise AssertionError('expected syntax failure')
    except (TypeError, ValueError) as error:
        print('syntax-error', repr(specification), type(error).__name__, str(error))
for value in ['12', float('nan'), float('inf')]:
    try:
        print('%04d' % value)
        raise AssertionError('expected conversion failure')
    except (TypeError, ValueError, OverflowError) as error:
        print('conversion-error', type(error).__name__, str(error))
print('decimal-supported-done')
