# ryupy: pending
# 类局部读取与方法free分离；私名和qualname按最近词法类及定义路径计算。
x = 'module'
def outer():
    x = 'outer'
    class Parent:
        x = x
        y = 'class'
        def read(self, default=y):
            return x, default
        class Inner:
            def read(self):
                return x, __class__
    x = 'late'
    return Parent

parent = outer()
print(parent.x, parent().read())
print(parent.Inner().read()[0], parent.Inner().read()[1] is parent.Inner)
print(parent.__qualname__, parent.Inner.__qualname__)
print(parent.__module__ == __name__, parent.Inner.__module__ == __name__)

def missing():
    no_global = 3
    class Missing:
        no_global = no_global
    return Missing

try:
    missing()
except NameError:
    print('class local does not read outer local')

# 即使没有方法捕获，类体free读取也是真实外层cell；不能改成LOAD_FAST。
def forward_free():
    class Forward:
        value = later
    later = 1
    return Forward
try:
    forward_free()
except NameError as error:
    print(type(error).__name__)

def annotation_outer():
    annotation = int
    def make():
        class Annotated:
            value: annotation
        return Annotated
    return make
print(annotation_outer()().__annotations__['value'] is int)
