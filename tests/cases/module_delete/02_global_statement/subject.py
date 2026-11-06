# 显式 global del 修改定义模块，失败时不能继续执行后续副作用。
value: int = 17
events = []

def read() -> int:
    return value

def remove():
    global value
    events.append('before')
    del value
    events.append('after')

def restore(replacement: int):
    global value
    value = replacement

# 未声明赋值的 global 删除也必须产生 NameError，不能成为无操作。
def remove_never_defined():
    global never_defined
    del never_defined

left = ['left']
right = ['right']

def remove_partial():
    global left, missing_middle, right
    del left, missing_middle, right
