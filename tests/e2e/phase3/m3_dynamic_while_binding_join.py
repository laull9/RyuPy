# 动态while的出口绑定合同：零次执行、else、break/continue、finally及删除/except清理保持真实未绑定状态。
events = []


def no_seed(values):
    index = 0
    while index < len(values):
        value = values[index]
        index += 1
    else:
        return value


def with_seed(values):
    value = 5
    index = 0
    while index < len(values):
        value = values[index]
        index += 1
    else:
        return value


def deleted_break():
    value = 5
    while True:
        del value
        break
        value = 6
    return value


def cleaned_handler():
    value = 5
    index = 0
    while index < 1:
        try:
            raise ValueError("clear-local")
        except ValueError as value:
            pass
        index += 1
    else:
        return value


def continue_finally():
    value = 5
    index = 0
    while index < 2:
        try:
            if index == 0:
                value = 9
                continue
            del value
            continue
        finally:
            events.append(index)
            index += 1
    else:
        return value


def deleted_else():
    value = 5
    while False:
        pass
    else:
        del value
    return value


def deleted_backedge():
    value = 5
    index = 0
    while index < 2:
        print("backedge", value)
        del value
        index += 1


for function, args in [(no_seed, [[]]), (no_seed, [[22]]), (no_seed, [[None]]),
                       (with_seed, [[]]), (with_seed, [[None]]),
                       (deleted_break, []), (cleaned_handler, []),
                       (continue_finally, []), (deleted_else, []), (deleted_backedge, [])]:
    try:
        print("value", function(*args))
    except UnboundLocalError as error:
        print("unbound", type(error).__name__)
print("cleanup", events)
