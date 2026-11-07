# ryupy: pending
# 原生符号快捷路径不能跳过当前缓存，也不能改写旧绑定。
import sys
import math
from math import sqrt as old_sqrt
original_math = math
import mc_replacement
sys.modules['math'] = mc_replacement
import math as current
from math import sqrt as new_sqrt, pi as new_pi
assert current is mc_replacement and new_sqrt is mc_replacement.sqrt
assert new_sqrt(9.0) == 109.0 and current.sqrt(9.0) == 109.0
assert new_pi == 17.0
assert old_sqrt(9.0) == 3.0 and original_math.sqrt(9.0) == 3.0
def local_import():
    import math
    from math import sqrt
    return math.sqrt(4.0), sqrt(4.0)
assert local_import() == (104.0, 104.0)
sys.modules['math'] = None
try:
    import math as blocked
except ModuleNotFoundError as exc:
    assert str(exc) == 'import of math halted; None in sys.modules'
else:
    raise AssertionError('native import must respect None')
sys.modules['math'] = original_math
import math as restored
assert restored is original_math and restored.sqrt(9.0) == 3.0
assert new_sqrt(9.0) == 109.0
print('native function/constant imports respect replacement', True)
