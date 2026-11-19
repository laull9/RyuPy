# 供跨模块默认值更新测试导入；原始函数无默认值，签名标注仍不能限制Python对象。
def required(value: int) -> int:
    return value
