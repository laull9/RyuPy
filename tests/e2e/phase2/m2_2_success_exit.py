# 正常返回须明确退出 0：不能依赖未初始化的 CIL 局部槽位。
try:
    raise ValueError("已处理")
except ValueError:
    print("成功")
finally:
    print("已清理")
