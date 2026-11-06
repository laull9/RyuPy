# 在默认辅助模块中检查实例函数异常，不放宽实例 except 预检。
observations = []
def inspect(fn):
    assert fn(None, lambda: 'shadow') == 'shadow'
    try:
        fn(None, super)
    except RuntimeError as exc:
        assert str(exc) == 'super(): empty __class__ cell'
        observations.append('empty')
    else:
        assert False
    return fn
def inspect_no_arguments(fn):
    try:
        fn()
    except RuntimeError as exc:
        assert str(exc) == 'super(): no arguments'
        observations.append('no arguments')
    else:
        assert False
    return fn
