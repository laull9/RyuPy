# 独立窄例：函数及方法绑定合同，源模块只有固定初始化。
import sys
import importlib.util as util
import subject
spec = subject.__spec__
factory = util.module_from_spec
aliases = [factory]
assert factory is util.module_from_spec
clone = aliases[0](spec=spec)
assert clone.__spec__ is spec and '__builtins__' not in clone.__dict__
create = spec.loader.create_module
execute = spec.loader.exec_module
assert create(spec) is None
assert execute(clone) is None
assert clone.source_marker == 'A' and clone.read() == 17
assert clone.read.__module__ == 'subject'
def expect(call, message):
    try:
        call()
    except TypeError as error:
        assert str(error) == message
    else:
        raise AssertionError('应当抛出TypeError')
expect(lambda: factory(), "module_from_spec() missing 1 required positional argument: 'spec'")
expect(lambda: factory(spec, spec), 'module_from_spec() takes 1 positional argument but 2 were given')
expect(lambda: factory(spec, spec=spec), "module_from_spec() got multiple values for argument 'spec'")
expect(lambda: factory(spec, spec, unknown=1), "module_from_spec() got an unexpected keyword argument 'unknown'")
expect(lambda: factory(unknown=1), "module_from_spec() got an unexpected keyword argument 'unknown'")
clone.value = 23
assert clone.read() == 23 and subject.read() == 17
assert sys.modules['subject'] is subject and subject.source_marker == 'A'
print('窄例：函数别名、保存的方法及绑定错误优先级正确')
