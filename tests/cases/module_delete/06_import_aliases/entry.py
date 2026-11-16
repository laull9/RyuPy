# ryupy: pending
# 模块顶层真实删除 import 绑定，已保存引用和提供方对象身份不变，重新导入恢复同一对象。
import subject
import provider
assert subject.saved_module is provider
assert subject.saved_function is provider.work
assert subject.saved_module.work is subject.saved_function
assert subject.saved_function(3) == 6
for operation, name in ((subject.read_module, 'imported'),
                        (subject.read_function, 'imported_work')):
    try:
        operation()
    except NameError as error:
        assert str(error) == "name '" + name + "' is not defined"
        print('deleted import binding', type(error).__name__, str(error))
    else:
        assert False
subject.restore()
assert subject.read_module() is provider
assert subject.read_function() is provider.work
assert subject.saved_function is subject.read_function()
print('import identity survives delete and reimport', provider.work(5))
