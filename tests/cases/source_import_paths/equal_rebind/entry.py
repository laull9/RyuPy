# 待跑合同输入；未执行，不能作为已验 CP 事实。
def report(label, error):
    print(label, type(error).__name__, str(error))
    print(label + ':name', getattr(error, 'name', None))
    print(label + ':cause-none', error.__cause__ is None)
    print(label + ':context', type(error.__context__).__name__)
    print(label + ':suppress', error.__suppress_context__)

import sys
import pkg
original = pkg.__path__
directory = original[0]
pkg.__path__ = [directory]
print('R:new-list', pkg.__path__ is original)
import pkg.first
pkg.__path__ = (directory,)
import pkg.second
pkg.__path__ = [directory, directory]
import pkg.duplicate
print('R:duplicate', pkg.duplicate.value)
pkg.__path__ = original
original.clear()
try:
    import pkg.third
except Exception as error:
    report('R:empty', error)
print('R:uncached', 'pkg.third' in sys.modules)
original.append(directory)
import pkg.third
print('R:restored', pkg.first.value, pkg.second.value, pkg.third.value)
