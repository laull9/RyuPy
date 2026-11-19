# ryupy: pending
# 已保存函数和类保持身份，但原名字的调用和基类求值必须重新读取绑定。
import subject
saved_function = subject.work
saved_class = subject.Base
assert saved_function is subject.function_alias
assert saved_class is subject.class_alias
assert subject.invoke(2) == 3
assert subject.derive().marker == 'old'
subject.events.clear()
subject.remove()
for operation, name in ((subject.invoke_zero, 'work'), (subject.invoke_effect, 'work'),
                        (subject.derive, 'Base')):
    try:
        operation()
    except NameError as error:
        assert str(error) == "name '" + name + "' is not defined"
        print('deleted definition', type(error).__name__, str(error))
    else:
        assert False
assert subject.events == []
assert saved_function is subject.function_alias and saved_function(4) == 5
assert saved_class is subject.class_alias and saved_class().marker == 'old'

def replacement(value: int) -> int:
    return value + 100

class Replacement:
    marker = 'new'

subject.work = replacement
subject.Base = Replacement
assert subject.invoke(4) == 104
assert subject.derive().marker == 'new'
assert saved_function(4) == 5 and saved_class().marker == 'old'
print('saved definitions and rebound dispatch', True)
