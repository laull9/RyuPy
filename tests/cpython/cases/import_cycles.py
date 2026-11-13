# ryupy: pending
"""CP1b：Python 反向导入半初始化编译模块，并验证失败模块可重试。"""
import sys
import bridge_fixture

compiled_state = []
from bridge_fixture import circular

assert circular.compiled_state is compiled_state
assert circular.compiled_module is sys.modules['__main__']
assert compiled_state == ['Python 反向导入']
try:
    from bridge_fixture import fails_once
except RuntimeError as error:
    assert str(error) == '首次导入失败'
else:
    raise AssertionError('第一次导入应失败')
assert 'bridge_fixture.fails_once' not in sys.modules
from bridge_fixture import fails_once
assert fails_once.attempt == 2
assert bridge_fixture.import_attempts == 2
print('CP1b 双向循环导入与失败重试通过')
