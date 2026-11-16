"""C4-2 辅助子模块：供相对导入与 live globals 读取使用。"""


def scale_value(base: int) -> int:
    """按固定倍率放大输入，供子模块验证相对导入。"""
    return base * 10
