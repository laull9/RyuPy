# 别名与保存的绑定方法不能依赖编译期函数名识别。
import sys
import importlib.util as util
import subject
saved = util.module_from_spec
functions = [saved]
spec = subject.__spec__
assert saved is util.module_from_spec
clone = functions[0](spec=spec)
create = spec.loader.create_module
execute = spec.loader.exec_module
assert create(spec) is None
assert execute(clone) is None and clone.read() == 41
assert clone.read.__module__ == 'subject'
def expect(call, message):
    try:
        call()
    except TypeError as error:
        assert str(error) == message
    else:
        raise AssertionError('应当抛出TypeError')
expect(lambda: saved(), "module_from_spec() missing 1 required positional argument: 'spec'")
expect(lambda: saved(spec, spec), 'module_from_spec() takes 1 positional argument but 2 were given')
expect(lambda: saved(spec, spec=spec), "module_from_spec() got multiple values for argument 'spec'")
expect(lambda: saved(spec, spec, unknown=1), "module_from_spec() got an unexpected keyword argument 'unknown'")
expect(lambda: saved(unknown=1), "module_from_spec() got an unexpected keyword argument 'unknown'")
assert sys.modules['subject'] is subject and subject.runs == 1
print('函数别名、关键字、绑定方法及参数错误优先级正确')
