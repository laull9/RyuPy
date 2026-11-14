# 默认来源联合合同；输出事实，不按解释器伪造成功。
import importlib
from importlib import metadata
import sys
import json
import os.path
import probe_pkg
from probe_pkg import state

class Finder:
    def find_spec(self, fullname, path=None, target=None):
        raise ValueError("finder-called")

def semantic():
    global probe_pkg, importlib
    points = metadata.entry_points(group="probe.active")
    print("cold", len(points), state.events, hasattr(probe_pkg, "lazy"))
    saved = importlib.import_module
    calls = []
    def rebound(name, package=None):
        calls.append(name)
        return probe_pkg
    importlib.import_module = rebound
    result = importlib.import_module("probe_pkg.lazy")
    importlib.import_module = saved
    print("rebound", result is probe_pkg, calls, state.events)
    for point in points:
        if point.name == "lazy":
            print("loaded", point.load())
    first = importlib.import_module("probe_pkg.lazy")
    second = importlib.import_module("probe_pkg.lazy")
    print("cache", first is second, probe_pkg.lazy is first, state.events)
    try:
        import probe_pkg.flaky
    except ValueError as error:
        print("failed", str(error), "probe_pkg.flaky" in sys.modules,
              hasattr(probe_pkg, "flaky"), "probe_pkg.dependency" in sys.modules)
    import probe_pkg.flaky
    print("retry", state.attempts, state.events, probe_pkg.flaky.value)
    parent_dir = os.path.dirname(probe_pkg.__file__)
    print("locations", os.path.basename(probe_pkg.__file__) == "__init__.py",
          probe_pkg.__path__[0] == parent_dir,
          os.path.dirname(first.__file__) == parent_dir,
          os.path.basename(first.__file__) == "lazy.py")

    # 真实catalog ModuleFile副本；不冒称Package父路径授权已被消费。
    import html.parser
    import importlib.util
    original = html.parser
    saved_class = original.HTMLParser
    saved_cached = sys.modules['html.parser']
    spec = importlib.util.find_spec('html.parser')
    copied = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(copied)
    print("copy", copied is not original, copied.__spec__ is spec,
          copied.__name__ == 'html.parser', copied.__package__ == 'html',
          copied.__file__ == spec.origin, copied.HTMLParser is not saved_class,
          copied.HTMLParser.__module__ == 'html.parser')
    saved_copy_class = copied.HTMLParser
    copied.interesting_normal = 'sentinel'
    print("copy-isolation", original.interesting_normal != 'sentinel')
    spec.loader.exec_module(copied)
    print("copy-reexec", copied.HTMLParser is not saved_copy_class,
          original.HTMLParser is saved_class, sys.modules['html.parser'] is saved_cached,
          html.parser is original)

def boundary():
    global probe_pkg
    points = metadata.entry_points(group="probe.active")
    print("cold", len(points), state.events)
    for point in points:
        if point.name == "deferred":
            try:
                print("deferred", point.load())
            except NotImplementedError:
                print("deferred", "NI")
    # group来自运行期字符串构造；不能再成为literal已选group。
    other = ".".join(["probe", "other"])
    for point in metadata.entry_points(group=other):
        try:
            print("outside", point.load())
        except NotImplementedError:
            print("outside", "NI")
    saved_path = probe_pkg.__path__
    probe_pkg.__path__ = []
    try:
        import probe_pkg.path_empty
    except ModuleNotFoundError as error:
        print("empty", error.name, "path_empty" in state.events)
    finally:
        probe_pkg.__path__ = saved_path
    import probe_pkg.path_empty
    probe_pkg.__path__ = [saved_path[0] + "/not-the-parent"]
    try:
        import probe_pkg.path_changed
    except ModuleNotFoundError:
        print("changed", "missing")
    except NotImplementedError:
        print("changed", "NI")
    finally:
        probe_pkg.__path__ = saved_path
    # finder最后运行；整个进程退出，不需要删除原本不存在的属性。
    sys.meta_path = [Finder()]
    try:
        import probe_pkg.finder_child
    except ValueError as error:
        print("finder", str(error))
    except NotImplementedError:
        print("finder", "NI")
    print("events", state.events)
