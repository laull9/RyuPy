# 导入别名属于函数词法绑定，global/nonlocal 复用相同模块槽/cell。
math = '外层名字'


def local():
    try:
        print(math)
    except UnboundLocalError:
        print('导入是局部绑定')
    import math
    return math.sqrt(9)


print(local(), math)


def global_import():
    global imported_math
    import math as imported_math


global_import()
print(imported_math.sqrt(16))


def outer():
    alias = '旧值'

    def replace():
        nonlocal alias
        from math import sqrt as alias

    replace()
    return alias(25)


print(outer())


class Guard:
    def __enter__(self):
        print('进入')
        return self

    def __exit__(self, typ, value, trace):
        print('清理', typ is ModuleNotFoundError)
        return False


def guarded():
    try:
        with Guard():
            import ryupy_definitely_missing_local_dependency
    except ModuleNotFoundError:
        print('外层捕获')


guarded()


def paused():
    yield '导入前'
    import math
    yield math.sqrt(36)


generator = paused()
print(next(generator), next(generator))
