# 有界环境探针：只用既有函数、全局、闭包与生成器语法，不实现新能力。
value = 10

def read():
    return value

def capture():
    def inner():
        return token
    return inner

def stream():
    yield value
    yield value

def remove():
    global value
    del value
