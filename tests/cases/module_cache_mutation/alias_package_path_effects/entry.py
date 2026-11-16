# ryupy: pending
# CP正期待：别名包动态后代导入使用父包当前path，并把新子模块附着到同一父对象。
import sys
import mc_state
import mc_probe
import mc_pkg
importer = mc_probe.marker.__builtins__['__import__']
package = mc_pkg
original_path = package.__path__
original_spec = package.__spec__
class ObservedPath:
    def __iter__(self):
        mc_state.events.append('path:iter')
        return iter(original_path)
package.__path__ = ObservedPath()
alias = 'mc_path_package_alias'
sys.modules[alias] = package
assert mc_state.events == ['parent'] and mc_state.runs == 0
assert importer(alias + '.child') is package
alias_child = sys.modules[alias + '.child']
assert alias_child is package.child and alias_child.__name__ == alias + '.child'
assert alias_child.__spec__.name == alias + '.child'
assert alias_child.value == 1 and mc_state.runs == 1
assert 'path:iter' in mc_state.events
assert mc_state.events[-1] == 'child:' + alias + '.child'
assert 'mc_pkg.child' not in sys.modules
assert package.__name__ == 'mc_pkg' and package.__spec__ is original_spec
print('alias child lookup trace', mc_state.events)
# 真实静态import登记原子模块源；其运行发生在别名子模块之后。
import mc_pkg.child as original_child
assert original_child is not alias_child and original_child.value == 2
assert original_child is package.child and original_child is sys.modules['mc_pkg.child']
assert original_child.__name__ == 'mc_pkg.child' and mc_state.runs == 2
assert sys.modules[alias + '.child'] is alias_child and alias_child.value == 1
print('source child lookup trace', mc_state.events)
before_cached = list(mc_state.events)
assert importer(alias + '.child', None, None, (), 0) is package
assert mc_state.events == before_cached and mc_state.runs == 2
assert package.child is original_child and sys.modules[alias + '.child'] is alias_child
package.__path__ = original_path
print('alias child cache/parent binding/path effects remain distinct', True)
