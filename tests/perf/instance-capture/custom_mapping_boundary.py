# 任意自定义mapping的直接**边界，与方法预捕获分离；保持同源CP/基线/候选精确对照。
# sink是普通函数，避免用方法目标重绑解释这个既有参数展开缺口。
events = []

class Keywords:
    def keys(self):
        events.append('keys')
        return ['x']
    def __getitem__(self, key):
        events.append('item:' + key)
        return 8

def sink(**kwargs):
    events.append('body')
    return kwargs['x']

try:
    print('boundary-result', sink(**Keywords()), events)
except TypeError as error:
    print('boundary-error', str(error), events)
