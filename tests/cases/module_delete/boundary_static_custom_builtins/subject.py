# 当前模块 frame 保留入口 builtins；之后定义的函数捕获当时 __builtins__ 字典。
saved_len = len

def replacement(sequence):
    return 70

custom = {'len': replacement}
__builtins__ = custom
len = 100

def measure(sequence):
    return len(sequence)

def remove_again():
    global len
    del len

del len
top_level = len([1, 2])
