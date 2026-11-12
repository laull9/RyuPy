# 导入名、转导出、普通别名和模块属性均按当时真实基值创建类。
from bases import Diamond as Imported
from exports import Exported
import bases

class First(Imported):
    def chain(self):
        return ['First'] + super().chain()

Alias = Exported
class Second(Alias):
    pass
Alias = bases.Other
class Third(Alias):
    pass
class Fourth(bases.Diamond):
    pass
bases.Diamond = bases.Other
class Fifth(bases.Diamond):
    pass
print(First().chain(), Second().chain(), Third().chain())
print(Fourth().chain(), Fifth().chain())
print(issubclass(First, Imported), issubclass(Second, Exported))
print([c.__name__ for c in First.__mro__])

# 最外层下标表达式属于现有动态基语法；检查只取一次和类体前的顺序。
events = []
class Selector:
    def __getitem__(self, key):
        events.append(('base', key))
        return Exported
selector = Selector()
class Sixth(selector['chosen']):
    events.append(('body', 'Sixth'))
print(events, Sixth().chain())

# 调用仅作为属性接收者；不扩大为直接调用表达式基类。
class Namespace:
    pass
namespace = Namespace()
namespace.base = Exported
def get_namespace():
    events.append('receiver')
    return namespace
class Seventh(get_namespace().base):
    events.append('seventh-body')
print(events, Seventh().chain())
