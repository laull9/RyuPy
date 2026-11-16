"""读取随第三方包分发的中文资源，项目无需手写payload声明。"""
from receipt_assets import label


def heading():
    return label()
