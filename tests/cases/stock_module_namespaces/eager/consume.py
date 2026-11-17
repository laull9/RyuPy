# 另一个编译模块原地改真实D；后续函数/推导式读取同一存储。
import growth

def observe():
    growth.__dict__['factor'] = 7
    return growth.current([2, 3]), growth.before('abc'), growth.after('abc'), growth.under_function(['x'])
