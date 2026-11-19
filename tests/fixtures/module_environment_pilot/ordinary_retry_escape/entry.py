# 普通import失败后的旧函数/旧dict存活与新环境重试完整见证。
import sys
import control
try:
    import subject
except RuntimeError as error:
    assert str(error) == 'ordinary-failure: escaped owner'
    print('exception prefix', str(error))
else:
    raise AssertionError('first import must fail')
old = control.saved[0]
old_function = control.functions[0]
old_dict = control.dictionaries[0]
old_value = old_function()
assert old_function.__globals__ is old_dict and old.__dict__ is old_dict
assert old_value == [1] and old_dict['partial'] == 'written'
assert 'completed' not in old_dict and 'subject' not in sys.modules
assert sys.modules['control'] is control and control.events == [1]
import subject
assert subject is not old and subject.__dict__ is not old_dict
assert subject.read is not old_function
assert subject.read.__globals__ is subject.__dict__
assert subject.read() == [2] and subject.read() is not old_value
assert subject.completed is True and subject.attempt == 2
assert old_function() is old_value and old_value == [1]
assert 'completed' not in old_dict and old_dict['attempt'] == 1
old_dict['value'] = None
assert old_function() is None and subject.read() == [2]
subject.value = 'fresh'
assert old_function() is None and subject.read() == 'fresh'
assert sys.modules['control'] is control and control.events == [1, 2]
assert control.saved == [old, subject] and control.attempts == 2
assert sys.modules['subject'] is subject
import subject as again
assert again is subject and control.attempts == 2
print('escaped old function and old dictionary preserved')
print('retry new dictionary successful dependency preserved')
