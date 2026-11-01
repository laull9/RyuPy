print('子模块初始化')
value = 9


def relative():
    from . import sibling
    return sibling.value
