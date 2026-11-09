# 待跑合同输入；未执行，不能作为已验 CP 事实。
def report(label, error):
    print(label, type(error).__name__, str(error))
    print(label + ':name', getattr(error, 'name', None))
    print(label + ':cause-none', error.__cause__ is None)
    print(label + ':context', type(error.__context__).__name__)
    print(label + ':suppress', error.__suppress_context__)

import sys
import state
import pkg
original = pkg.__path__
try:
    import pkg.child
except Exception as error:
    report('F:first', error)
    print('F:identity', error is state.failure)
print('F:after-fail', state.attempts, 'pkg.child' in sys.modules, 'dependency' in sys.modules)
pkg.__path__ = []
try:
    import pkg.child
except Exception as error:
    report('F:blocked-retry', error)
print('F:after-block', state.attempts, 'pkg.child' in sys.modules)
pkg.__path__ = original
import pkg.child
import dependency
print('F:success', state.attempts, pkg.child.value, state.dependency is dependency, hasattr(pkg, 'child'))
