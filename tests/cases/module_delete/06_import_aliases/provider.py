# 提供真实模块对象和函数，消费方删别名不应影响这里的导出。
def work(value: int) -> int:
    return value * 2
