"""C4-2 循环导入方 B：反导仍在初始化的 A，验证部分初始化对象可见性。"""
from cil_pkg import PKG_TRACE
import cil_pkg.cyclic_a as cyclic_a

PKG_TRACE.append("b:enter")
FROM_A_BEFORE = cyclic_a.BEFORE_B * 2
HAS_AFTER_YET = hasattr(cyclic_a, "AFTER_B")
PKG_TRACE.append("b:exit")
