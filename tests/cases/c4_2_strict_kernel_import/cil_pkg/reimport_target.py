"""C4-2 删除 sys.modules 后重新导入生成新身份目标模块。"""
from cil_pkg import PKG_TRACE

GENERATION = len([item for item in PKG_TRACE if item.startswith("reimport:")]) + 1
PKG_TRACE.append(f"reimport:{GENERATION}")
TOKEN = f"gen-{GENERATION}"
