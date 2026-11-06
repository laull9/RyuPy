# delattr公开合同：保存builtin、严格绑定、别名、property、副作用与暂停callee。
saved = delattr
assert saved is delattr and callable(saved) and saved.__name__ == 'delattr'
class Box:
    @property
    def readonly(self):
        return get_item(self)
value = Box()
alias = value
item = []
value.item = item
value.keep = item
assert saved(value, 'item') is None
assert not hasattr(alias, 'item') and alias.keep is item
for name in ('', 'not-an-identifier', '中文', 'a\0b'):
    setattr(value, name, item)
    assert delattr(value, name) is None and not hasattr(value, name)
class AttributeName(str):
    pass
value.item = item
assert saved(value, AttributeName('item')) is None and not hasattr(value, 'item')
print('delete.identity', True)
def reject(operation, expected):
    try:
        operation()
    except Exception as error:
        assert type(error) is expected
        return str(error)
    raise AssertionError('invalid call accepted')
for args in ((), (value,), (value, 'keep', 1), (value, 'keep', 1, 2)):
    assert reject(lambda: saved(*args), TypeError) == 'delattr expected 2 arguments, got ' + str(len(args))
assert reject(lambda: delattr(value, 'keep', 1, name='keep'), TypeError) == 'delattr() takes no keyword arguments'
assert reject(lambda: saved(value, 1), TypeError) == "attribute name must be string, not 'int'"
assert value.keep is item
assert reject(lambda: saved(value, 'missing'), AttributeError) == "'Box' object has no attribute 'missing'"
print('delete.binding', True)
Box.class_item = item
assert value.class_item is item
reject(lambda: delattr(value, 'class_item'), AttributeError)
assert delattr(Box, 'class_item') is None and not hasattr(value, 'class_item')
def function():
    pass
function.item = item
function_alias = function
assert saved(function, 'item') is None and not hasattr(function_alias, 'item')
function.__module__ = 'owner'
function.__dict__['__module__'] = 'shadow'
assert delattr(function, '__module__') is None and function.__module__ is None
assert function.__dict__['__module__'] == 'shadow'
print('delete.class-function', True)
events = []
def get_item(self):
    events.append('get')
    return 1
def delete_item(self):
    assert self is value
    events.append('delete')
    return 9
Box.property_item = property(get_item).deleter(delete_item)
assert saved(value, 'property_item') is None and events == ['delete']
readonly_message = reject(lambda: saved(value, 'readonly'), AttributeError)
assert readonly_message == "property 'readonly' of 'Box' object has no deleter", readonly_message
assert events == ['delete']
marker = LookupError('delete marker')
def fail_delete(self):
    events.append('fail')
    raise marker
Box.failed = property(get_item).deleter(fail_delete)
try:
    saved(value, 'failed')
except LookupError as error:
    assert error is marker and value.keep is item
else:
    raise AssertionError('deleter did not fail')
assert events == ['delete', 'fail']
print('delete.properties', events)
events = []
def arg(label, result):
    events.append(label)
    return result
reject(lambda: saved(arg('target', value), arg('name', 'keep'), arg('extra', 1)), TypeError)
assert events == ['target', 'name', 'extra'] and value.keep is item
def shadow(delattr):
    return delattr(value, 'keep')
assert shadow(lambda target, name: (target is value, name)) == (True, 'keep')
print('delete.evaluation', events)
# 模块别名改绑发生在yield之后，已准备的真实callee仍须调用原builtin。
suspend_delete = saved
def paused():
    return suspend_delete(value, (yield 'name'))
value.paused = item
iterator = paused()
assert next(iterator) == 'name'
suspend_delete = lambda target, name: 'wrong'
try:
    iterator.send('paused')
except StopIteration as done:
    assert done.value is None
else:
    raise AssertionError('paused call did not finish')
assert not hasattr(value, 'paused') and value.keep is item
print('delete.paused', True)
# 冻结完整用户帧、显式cause与处理中的context，不吞掉失败尾部。
def chain_delete(self):
    raise LookupError('delattr-deleter')
Box.chain = property(get_item).deleter(chain_delete)
def delete_middle():
    saved(value, 'chain')
def delete_outer():
    delete_middle()
try:
    raise ValueError('delattr-cause')
except ValueError as cause:
    saved_cause = cause
try:
    raise RuntimeError('delattr-ambient') from saved_cause
except RuntimeError:
    print('delete.chain', True)
    delete_outer()
