# 同源程序：生成函数内处理旧异常，再向模块传播替换异常。
def generate():
    try:
        raise ValueError("original")
    except ValueError:
        raise TypeError("replacement")

try:
    generate()
except TypeError as caught:
    result = caught
