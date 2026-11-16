"""C4-2 循环导入方 A：在初始化中途发布前置属性并导入 B。"""
import sys
from cil_pkg import PKG_TRACE

EXEC_COUNT = 0
EXEC_COUNT += 1
PKG_TRACE.append("a:enter")
BEFORE_B = 11

# 此处必须能被正在执行中的 B 通过 sys.modules 观察到已发布的模块对象与 BEFORE_B
import cil_pkg.cyclic_b as cyclic_b  # noqa: E402

AFTER_B = BEFORE_B + cyclic_b.FROM_A_BEFORE
SELF_IS_IN_SYS = sys.modules[__name__] is sys.modules["cil_pkg.cyclic_a"]
PKG_TRACE.append("a:exit")
