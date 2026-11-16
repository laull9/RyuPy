"""C1 prepare 必须先于类体元数据读取；真实 namespace 可遮蔽模块名。"""
from bridge_fixture import class_construction as helper
from bridge_fixture.class_construction import ShadowBase, ChangingBase

helper.reset()
original_name = __name__


class Shadow(ShadowBase):
    def method(self):
        return 42
    function_module = method.__module__


assert Shadow.__module__ == 'namespace_name'
assert Shadow.function_module == original_name
assert helper.snapshot()[1] == ['namespace:__name__']


def change_module_name():
    global __name__
    __name__ = 'changed_by_prepare'


helper.set_prepare_callback(change_module_name)


class Changing(ChangingBase):
    def method(self):
        return 17
    function_module = method.__module__


assert Changing.__module__ == 'changed_by_prepare'
assert Changing.function_module == 'changed_by_prepare'
__name__ = original_name


def nested():
    x = 'outer_cell'
    def outer_reader():
        return x
    class Local(ShadowBase):
        captured = x
        def method(self):
            return 23
    assert outer_reader() == 'outer_cell'
    return Local


Local = nested()
assert Local.__module__ == 'namespace_name'
assert Local.captured == 'namespace_cell'
assert Local.__qualname__ == 'nested.<locals>.Local'
assert Local().method() == 23
helper.set_prepare_callback(None)
print('native class prepare metadata passed')
