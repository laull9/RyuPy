# 只读取但未赋值的全局名需要预留槽；预留不代表模块属性已存在。
def read_missing():
    return injected_later


# 有语法赋值但从未执行的静态整数也不是已绑定属性。
if False:
    conditional_number = 7
