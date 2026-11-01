# ryupy: pending
# 普通源码类跨模块继承必须读取真实导入绑定，不固化跨模块静态类身份。
import fixtures.runtime_base_case.base as module
from fixtures.runtime_base_case import Base as Alias
from fixtures.runtime_base_case.child import Child
print(module.events, Child().value())
class Local(Alias):
    pass
print(Local().value(), isinstance(Local(), Alias))
# from 绑定保原值，模块属性重绑定则应读取新的普通类。
module.Base = module.Other
class Changed(module.Base):
    pass
print(Changed().value(), Local().value())
# 基类表达式中的属性访问仅一次，且在类体执行之前。
log = []
class Holder:
    @property
    def base(self):
        log.append('base')
        return Alias
holder = Holder()
class Once(holder.base):
    log.append('body')
print(log, Once().value())
# 支持的普通基类路径：类体失败保留公开旧绑定，但保留已经执行的副作用。
Existing = Alias
module.Base = module.Other
def fail_body():
    raise ValueError('body')
try:
    class Existing(module.Base):
        log.append('failing body')
        fail_body()
except ValueError:
    print('failed body', Existing is Alias, log)
# 装饰器表达式先于基类表达式，应用失败同样不能公开中间类对象。
def decorator_factory():
    log.append('decorator expression')
    def apply(cls):
        log.append('decorator apply')
        raise ValueError('decorator')
    return apply
try:
    @decorator_factory()
    class Existing(holder.base):
        log.append('decorated body')
except ValueError:
    print('failed decorator', Existing is Alias, log)
# 导入别名本身也可以重新绑定为普通类，不做静态身份锁定。
Alias = module.Other
class ReboundAlias(Alias):
    pass
print(ReboundAlias().value(), Local().value())
# 本地继续继承跨模块普通类，仍读最终公开槽；body失败不能抢先覆盖旧槽。
class Grandchild(Local):
    pass
print(Grandchild().value(), isinstance(Grandchild(), Local))
SavedGrandchild = Grandchild
try:
    class Grandchild(Local):
        fail_body()
except ValueError:
    print('grandchild preserved', Grandchild is SavedGrandchild)
