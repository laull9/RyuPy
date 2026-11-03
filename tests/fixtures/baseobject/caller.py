# 参数先求值，构造失败必须保留实际caller。
def marker():
    print('argument')
    return 1
def caller():
    object(marker())
caller()
