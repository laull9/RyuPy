# list.pop有限索引域：宽整数不截断，普通实例索引一次调用，失败无额外修改。
events = []


class Index:
    def __init__(self, value):
        self.value = value

    def __index__(self):
        events.append('index')
        return self.value


class Missing:
    pass


class IntOnly:
    def __int__(self):
        events.append('int must not run')
        return 0


def direct(values, index):
    return values.pop(index)


def saved(values, index):
    method = values.pop
    return method(index)


def expanded(values, index):
    return values.pop(*[index], **{})


# 每个普通方法调用位置反复执行，使PIC命中与冷入口接受同一合同。
for name, operation in [('direct', direct), ('saved', saved), ('expanded', expanded)]:
    for empty in [False, True]:
        for index in [0, -1, False, True, 2, -3, 4294967296, -4294967296,
                      9223372036854775807, -9223372036854775808,
                      9223372036854775808, -9223372036854775809,
                      Index(0), Index(-1), Index(4294967296),
                      Index(9223372036854775808), Index(-9223372036854775809),
                      Index(None), Index('bad'), 1.5, None,
                      Missing(), IntOnly()]:
            values = [] if empty else [10, 20]
            alias = values
            events.clear()
            try:
                result = operation(values, index)
                print(name, empty, type(index).__name__, 'returned', result, values,
                      alias is values, events)
            except Exception as error:
                print(name, empty, type(index).__name__, type(error).__name__,
                      str(error), values, alias is values, events)
print('WIDE_PROTOCOL_COMPLETED')
