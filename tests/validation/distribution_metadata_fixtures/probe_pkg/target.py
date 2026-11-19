# 此行只能在EntryPoint.load时出现，编译登记或发现不得打印。
print('target-executed')
def factory(value=7):
    return value + 1
class Box:
    value = 'initial'

def other_factory(value=7):
    return value + 100
