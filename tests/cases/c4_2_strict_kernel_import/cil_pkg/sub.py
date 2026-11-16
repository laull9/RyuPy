"""C4-2 子模块：验证相对导入与实时 __dict__/globals 同步。"""
from .helper import scale_value
from .. import VERSION

LIVE_COUNTER = scale_value(7)


def read_live_slot(key: str):
    """直接读取模块全局命名空间，验证与外部 module.__dict__ 物理同源。"""
    return globals()[key]


def write_live_slot(key: str, value) -> None:
    """从模块内部修改 globals()，验证外部 sys.modules 视图立即可见。"""
    globals()[key] = value


def describe() -> str:
    """返回包含上级包版本和计数的摘要字符串。"""
    return f"{VERSION}:{LIVE_COUNTER}"
