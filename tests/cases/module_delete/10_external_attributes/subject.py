# 默认模块删全局后，既有观察者看到缺失，外部赋值恢复其同一模块属性。
payload = ['old']

def remove():
    global payload
    del payload
