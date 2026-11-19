# ryupy: pending
# 正文不调用f；真实CP atexit保活F并在终态调用静态CIL业务体，函数仍保留于模块。
import atexit

def f(x):
    return x

atexit.register(f, 1000)
print("registered")
