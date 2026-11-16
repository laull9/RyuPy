# 未执行的隔离候选：真实函数元数据、定义时序、别名与专用成员。
# 不读取frame；下方显式函数属性读取不能冒充caller来源验证。
entry_name = __name__
def original(value=3):
    return value
saved = original
print('initial', original.__module__ == entry_name)
__name__ = 'before-definition'
def current(value=4):
    return value
__name__ = 'after-definition'
print('capture', saved.__module__ == entry_name, current.__module__, current())

def running(callback):
    callback()
    return running_saved.__module__
running_saved = running
def change_running():
    running_saved.__module__ = 'changed-during-body'
print('running-field', running(change_running), running_saved is running)
saved.__module__ = 'changed-original'
original = current
print('alias', saved.__module__, original.__module__, saved(), original())

marker = []
saved.__module__ = marker
saved.__dict__['__module__'] = 'dictionary-only'
print('descriptor', saved.__module__ is marker, saved.__dict__['__module__'])
saved.__dict__ = {'__module__': 'replacement-dictionary'}
print('dict-replacement', saved.__module__ is marker)
for value in [None, '', 0, False, marker]:
    saved.__module__ = value
    print('assignment-identity', saved.__module__ is value)
del saved.__module__
del saved.__module__
print('delete', saved.__module__ is None, saved.__dict__['__module__'])

__name__ = marker
def nonstring_module():
    return 5
print('nonstring-name', nonstring_module.__module__ is marker)
del __name__
def missing_name():
    return 6
print('missing-name', missing_name.__module__ is None)
__name__ = 'lambda-definition'
anonymous = lambda: 8
__name__ = 'after-lambda'
print('lambda', anonymous.__module__, anonymous())
instances = []
for definition_name in ['repeat-first', 'repeat-second']:
    __name__ = definition_name
    def repeated(value=[]):
        return value
    instances.append(repeated)
print('repeated-def', instances[0] is not instances[1], instances[0].__module__, instances[1].__module__, instances[0]() is not instances[1]())
__name__ = entry_name

events = []
raw_functions = []
def set_name(label, result):
    global __name__
    events.append(label)
    __name__ = label
    return result
def decorator(label):
    set_name('evaluate-' + label, None)
    def apply(function):
        events.append(['apply-' + label, function.__module__])
        if label == 'outer':
            return function
        raw_functions.append(function)
        set_name('wrapper-definition', None)
        def wrapper(value=11):
            return function(value)
        return wrapper
    return apply
@decorator('outer')
@decorator('inner')
def decorated(value: set_name('parameter-annotation', int) = set_name('pos-default', 7), *, flag: set_name('kw-annotation', str) = set_name('kw-default', 'flag')) -> set_name('return-annotation', int):
    return value
print('definition-events', events)
print('decorated', raw_functions[0].__module__, decorated.__module__, raw_functions[0](), decorated())
__name__ = entry_name
