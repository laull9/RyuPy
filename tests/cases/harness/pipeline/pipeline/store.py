# 模块状态：列表在多次导入之间共享，clear 通过 global 重新绑定。
items = []


def add(record):
    items.append(record)
    return len(items)


def total(factor):
    result = 0
    for record in items:
        result += record.scaled(factor)
    return result


def clear():
    global items
    items = []
