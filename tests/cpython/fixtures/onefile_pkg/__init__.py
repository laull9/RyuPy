"""OneFile 验收专用手写测试包。
提供基础包计算、资源访问辅助及元数据标识。
"""

NAME = "onefile_pkg"
VERSION = "1.0.0"


def get_greeting(target: str) -> str:
    """返回包含目标名称的问候语。"""
    return f"你好，{target}！来自 onefile_pkg 运行时。"


def compute_sum(a: int, b: int) -> int:
    """基础算术计算。"""
    return a + b
