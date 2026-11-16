# 名字删除必须真正移除实例字典键，包含只由外部注入而未在正文赋值的名字。
token = []
saved_token = token
del token
token = saved_token
del token
value = ['old']
len = 91

def read():
    return value

def remove():
    global value
    del value

def read_injected():
    return injected

def remove_injected():
    global injected
    del injected

def read_len():
    return len

def remove_len():
    global len
    del len
