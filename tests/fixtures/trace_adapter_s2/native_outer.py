# 同源：native错误穿过callee，由外层Python函数接收。
def native_callee():
    locals(1)

def native_outer():
    try:
        native_callee()
    except TypeError as caught:
        saved = caught
    return saved

result = native_outer()
