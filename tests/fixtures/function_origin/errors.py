# 未执行的失败发布与完整异常链应用；失败前后的metadata不能污染旧alias。
entry_name = __name__
__name__ = 'old-definition'
def selected():
    raise LookupError('old callable')
saved = selected
events = []
def fail(label):
    global __name__
    __name__ = label
    events.append(label)
    raise ValueError(label)
try:
    def selected(value=fail('default-failed')):
        return value
except ValueError as error:
    print('default-preserved', selected is saved, selected.__module__, str(error))
try:
    def selected(value: fail('annotation-failed')):
        return value
except ValueError as error:
    print('annotation-preserved', selected is saved, selected.__module__, str(error))
def reject(function):
    events.append(['rejected-module', function.__module__])
    raise ValueError('decorator-failed')
try:
    @reject
    def selected(value=1):
        return value
except ValueError as error:
    print('decorator-preserved', selected is saved, selected.__module__, str(error))
print('events', events)
try:
    before()
except NameError as error:
    print('before-def', type(error).__name__, str(error))
def before(value: int = 3) -> int:
    return value
print('after-def', before())
__name__ = entry_name
try:
    fail('final-default-failed')
except ValueError:
    saved.__module__ = 'changed-before-error'
    print('saved-before-error', saved.__module__)
    try:
        saved()
    except LookupError as cause:
        raise RuntimeError('outer failure') from cause
