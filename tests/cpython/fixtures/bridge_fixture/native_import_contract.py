"""CPython 侧真实导入消费者；函数体始终在外部 provider 执行域。"""
import atexit
import importlib
import sys
import types

counts = {}


def entered(name):
    counts[name] = counts.get(name, 0) + 1
    return counts[name]


def load(name):
    return importlib.import_module(name)


def native_finder():
    for finder in sys.meta_path:
        kind = type(finder)
        if kind.__module__ in ('_ryupy_imports', 'ryupy') and kind.__name__ in ('_RyuPyFinder', 'NativeFinder'):
            return finder
    raise AssertionError('missing internal compiled finder')


def describe(name):
    finder = native_finder()
    spec = finder.find_spec(name, None, None)
    return (type(finder).__module__, spec.name, spec.parent,
            spec.submodule_search_locations is not None,
            type(spec.loader).__module__)


def during_cycle(name, state):
    module = load(name)
    assert module.state is state
    state.append('callback')


def cache_overrides(name):
    original = load(name)
    try:
        sys.modules[name] = None
        try:
            load(name)
        except ModuleNotFoundError as error:
            assert error.name == name
        else:
            raise AssertionError('sys.modules None was ignored')
        replacement = types.ModuleType(name)
        sys.modules[name] = replacement
        assert load(name) is replacement
    finally:
        sys.modules[name] = original
    assert load(name) is original
    return original


def _at_shutdown():
    module = load('native_import_package.shutdown')
    assert module.value == 'shutdown import alive'
    assert counts['native_import_package.shutdown'] == 1
    print('NATIVE_IMPORT_SHUTDOWN_CONFIRMED')


def verify():
    # CPython 缓存先于 finder；未初始化目标的 None/替代值不能触发 CIL 正文。
    name = 'native_import_flat'
    sys.modules[name] = None
    try:
        load(name)
    except ModuleNotFoundError as error:
        assert error.name == name
    else:
        raise AssertionError('None cache accepted')
    replacement = types.ModuleType(name)
    sys.modules[name] = replacement
    assert load(name) is replacement
    del sys.modules[name]
    assert name not in counts
    flat = load(name)
    assert flat.value == 'flat module' and counts[name] == 1
    assert flat.__spec__.submodule_search_locations is None
    assert cache_overrides(name) is flat

    child = load('native_import_package.child')
    package = sys.modules['native_import_package']
    assert package.child is child and load(child.__name__) is child
    assert package.events == ['parent', 'child']
    assert counts[package.__name__] == counts[child.__name__] == 1
    assert package.__spec__.submodule_search_locations is not None
    assert package.__spec__.parent == package.__name__
    assert child.__spec__.parent == package.__name__
    empty = load('native_import_empty')
    assert empty.__spec__.submodule_search_locations is not None
    assert empty.__spec__.parent == 'native_import_empty'
    assert counts['native_import_empty'] == 1

    cycle = load('native_import_package.cycle')
    assert cycle.state == ['before', 'callback', 'after']
    assert load(cycle.__name__) is cycle and counts[cycle.__name__] == 1
    try:
        load('native_import_package.flaky')
    except RuntimeError as error:
        assert str(error) == 'native import retry'
    else:
        raise AssertionError('first failure was swallowed')
    assert 'native_import_package.flaky' not in sys.modules
    assert not hasattr(package, 'flaky')
    retried = load('native_import_package.flaky')
    assert retried.attempt == 2 and package.flaky is retried
    assert load(retried.__name__) is retried

    if '_ryupy_imports' in sys.modules:
        # 单独核实 CIL 执行归属，防止磁盘源码回落掩盖 native finder 缺失。
        assert not isinstance(child, types.ModuleType)
        finder = native_finder()
        assert type(finder).__module__ == 'ryupy'
        assert finder.find_spec('native_import_empty').submodule_search_locations is not None
        for attr in ('_RyuPyFinder', '_RyuPyLoader', '_rp_staticmethod', '_rp_classmethod', '_rp_property'):
            assert not hasattr(sys.modules['_ryupy_imports'], attr), attr
    atexit.register(_at_shutdown)
    return True


def repeat_exec(name, module):
    loader = native_finder().find_spec(name).loader
    assert loader.exec_module(module) is None
    assert loader.exec_module(module) is None
