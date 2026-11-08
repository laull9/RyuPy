# 同源：native错误由调用locals的当前Python函数接收。
def native_local():
    try:
        locals(1)
    except TypeError as caught:
        saved = caught
    return saved

result = native_local()
