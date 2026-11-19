# ryupy: pending
# 私名改写在单元1修订后属域外；保留原期望供后续完整mangling验收。
# 类局部读取与方法free分离；私名和qualname按最近词法类及定义路径计算。
x = 'module'
def outer():
    x = 'outer'
    class Parent:
        x = x
        y = 'class'
        __private = 7
        def read(self, default=y):
            return x, default, self.__private
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
print(parent._Parent__private)

def missing():
    no_global = 3
    class Missing:
        no_global = no_global
    return Missing

try:
    missing()
except NameError:
    print('class local does not read outer local')
