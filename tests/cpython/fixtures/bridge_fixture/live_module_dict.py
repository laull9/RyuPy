"""在 CPython 一侧检查编译入口的权威字典；回调期间的更改必须立即可见。"""


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
