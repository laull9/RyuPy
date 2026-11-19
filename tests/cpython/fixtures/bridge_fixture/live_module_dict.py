"""在 CPython 一侧检查编译入口的权威字典；回调期间的更改必须立即可见。"""


def namespace_fixture():
    """属性钩子不能影响 C API 取得模块真正的 md_dict。"""
    import types

    class Module(types.ModuleType):
        def __getattribute__(self, name):
            if name == "__dict__":
                raise AssertionError("不应执行用户字典属性钩子")
            return super().__getattribute__(name)

    return Module("live_namespace_fixture")


def raw_namespace(module):
    import types
    return types.ModuleType.__dict__["__dict__"].__get__(module)


def external_set(module, name, value):
    raw_namespace(module)[name] = value


def external_get(module, name):
    return raw_namespace(module)[name]


def external_delete(module, name):
    del raw_namespace(module)[name]


def mutate_entry(module, namespace, expected):
    """无编译函数入口的独立闭合合同，回调内直接读取同一真实dict。"""
    assert type(namespace) is dict
    assert namespace is module.__dict__
    assert namespace["shared"] is expected
    namespace["counter"] = 41
    assert module.counter == 41
    module.counter = 42
    assert namespace["counter"] == 42
    del module.counter
    assert "counter" not in namespace
    try:
        del namespace["counter"]
    except KeyError as error:
        assert error.args == ("counter",)
    else:
        raise AssertionError("删除缺失键必须抛 KeyError")
    namespace["counter"] = 43
    return namespace


def mutate(module, read, write, remove, expected):
    namespace = module.__dict__
    assert type(namespace) is dict
    assert namespace is module.__dict__
    assert read.__globals__ is namespace
    assert write.__globals__ is namespace
    assert remove.__globals__ is namespace
    assert namespace["shared"] is expected
    namespace["shared"] = expected
    namespace["counter"] = 41
    assert read() == 41
    write(42)
    assert namespace["counter"] == 42
    assert module.counter == 42
    module.counter = 43
    assert namespace["counter"] == 43
    remove()
    assert "counter" not in namespace
    try:
        read()
    except NameError:
        pass
    else:
        raise AssertionError("删除后仍读取了旧全局槽")
    namespace["counter"] = 44
    assert read() == 44
    del namespace["counter"]
    try:
        del module.counter
    except AttributeError:
        pass
    else:
        raise AssertionError("重复删除属性必须抛 AttributeError")
    try:
        del namespace["counter"]
    except KeyError as error:
        assert error.args == ("counter",)
    else:
        raise AssertionError("重复删除字典键必须抛 KeyError")
    namespace["counter"] = 45
    try:
        module.__dict__ = {}
    except AttributeError:
        pass
    else:
        raise AssertionError("模块字典属性必须只读")
    assert namespace is module.__dict__
    return namespace
