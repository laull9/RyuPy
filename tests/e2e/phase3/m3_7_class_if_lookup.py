# 条件未绑定的类名字惰性回退；类裸名不沿基类查找，None 是已绑定值。
module_value = 31
none_value = 77
inherited = 'module'
method_value = 'module method'


class Base:
    inherited = 'base'
    only_in_base = 'base only'


class Lookup(Base):
    if False:
        module_value = 0
        len = 0
        none_value = 0
        only_in_base = 0
        inherited = 'wrong'
    from_module = module_value
    from_builtin = len([1, 2, 3])
    from_module_not_base = inherited
    if True:
        none_value = None
        missing_everywhere = 'present'
        method_value = 'class method'
    bound_none = none_value
    no_eager_fallback = missing_everywhere
    def method(self, default=module_value):
        return (default, method_value)


print(Lookup.from_module, Lookup.from_builtin, Lookup.from_module_not_base)
print(Lookup.bound_none is None, Lookup.no_eager_fallback)
print(Lookup().method())
print('module_value' in Lookup.__dict__, 'len' in Lookup.__dict__)

try:
    class Missing(Base):
        if False:
            only_in_base = 'not assigned'
        value = only_in_base
except NameError:
    print('基类不是裸名回退来源')

try:
    class Unknown:
        if False:
            missing_name = 0
        value = missing_name
except NameError:
    print('所有命名空间均缺失')


class Annotations:
    before = __annotations__
    if False:
        never_bound: UnknownAnnotation
    after = __annotations__


print(Annotations.__annotations__ == {})
print(Annotations.before is Annotations.after)
print('never_bound' in Annotations.__dict__)

# 后置 dataclass 读取实际注解，不把未执行的声明静态并入字段。
from dataclasses import dataclass, fields


class LaterDataclass:
    if True:
        selected: int
    if False:
        absent: str


LaterDataclass = dataclass(LaterDataclass)
print([field.name for field in fields(LaterDataclass)])
print(LaterDataclass(23).selected)


class DeletedAnnotations:
    if True:
        selected: int


del DeletedAnnotations.__annotations__
DeletedAnnotations = dataclass(DeletedAnnotations)
print(len(fields(DeletedAnnotations)), type(DeletedAnnotations()) is DeletedAnnotations)
