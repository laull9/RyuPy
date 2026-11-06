# 该依赖未选择实例模式，真实回调仍通过传入模块对象修改其同一字典。
def alternative(value):
    return 91

def inject(module):
    module.__dict__['never_declared'] = ['injected']
    module.annotated = 'changed type'

def custom_range(value):
    return [7, 8]
