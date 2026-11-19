# ryupy: pending
# 真实CIL长入口循环；每轮临时原生整数可以自然回收，同一参数往返身份必须保留。
import sys

def f(x):
    return x

for i in range(int(sys.argv[1])):
    assert f(i) is i
print("ok")
