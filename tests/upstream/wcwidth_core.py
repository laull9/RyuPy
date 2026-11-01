# 基于 wcwidth 0.2.13 tests/test_core.py 的核心场景移植；MIT 许可见 licenses/wcwidth-0.2.13.txt。
# 保留原测试的逐字符与总宽度断言，移除 pytest/发行元数据依赖；不是整个上游测试套件。
import wcwidth


def check(phrase, each, total):
    assert tuple(map(wcwidth.wcwidth, phrase)) == each
    assert wcwidth.wcswidth(phrase) == total


assert wcwidth.wcwidth('') == 0
assert wcwidth.wcswidth('') == 0
check('hello\x00world', (1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1), 10)
check('コンニチハ, セカイ!', (2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 1), 19)
assert wcwidth.wcswidth('コンニチハ, セカイ!', 7) == 12
check('abc\x00def', (1, 1, 1, 0, 1, 1, 1), 6)
check('\x1b[0m', (-1, 1, 1, 1), -1)
check('--\u05bf--', (1, 1, 0, 1, 1), 4)
check('cafe\u0301', (1, 1, 1, 1, 0), 4)
check('\u0410\u0488', (1, 0), 1)
print('wcwidth core: 9 upstream scenarios passed')
