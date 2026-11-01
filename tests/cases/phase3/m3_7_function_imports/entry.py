# 图发现应包含函数体依赖，但不得在定义函数时执行它们。
def load():
    import lazy
    return lazy


print('调用前')
first = load()
second = load()
print(first is second, first.value)


def dotted():
    import pkg.part
    import pkg.part as alias
    print(pkg.part is alias, alias.value, alias.relative())


dotted()


def partial_from():
    kept = '旧值'
    try:
        from lazy import value as first, absent as kept
    except ImportError:
        print('逐项绑定', first, kept)
    try:
        from lazy import value as successful, absent as unbound
    except ImportError:
        print('成功前项', successful)
        try:
            print(unbound)
        except UnboundLocalError:
            print('失败后项未绑定')


def failures():
    previous = '保留'
    try:
        import broken as previous
    except ValueError:
        print('失败保旧', previous)
    try:
        import broken as fresh
    except ValueError:
        try:
            print(fresh)
        except UnboundLocalError:
            print('失败仍未绑定')


partial_from()
failures()


def circular():
    import cycle_a
    return cycle_a.load()


print(circular())


def never():
    import never_loaded
    return never_loaded


def capture_local_import():
    import lazy as captured

    def read():
        return captured.value

    return read


print('捕获导入', capture_local_import()())
