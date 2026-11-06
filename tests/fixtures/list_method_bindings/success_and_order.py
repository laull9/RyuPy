# exact list四方法的有限成功域、所有实参副作用及暂停绑定补足；不开放自定义索引。
events = []


def touch(label, value):
    events.append(label)
    return value


def report(label, values, operation):
    events.clear()
    alias = values
    try:
        result = operation(values)
        print(label, 'returned', result, values, alias is values, events)
    except Exception as error:
        print(label, type(error).__name__, str(error), values, alias is values, events)


def clear_zero(values):
    return values.clear()


def reverse_zero(values):
    return values.reverse()


def copy_zero(values):
    return values.copy()


def pop_zero(values):
    return values.pop()


# 空/单元素/普通接收者走直接小参数入口；pop空表异常不应变成绑定错误。
for label, operation in [('clear', clear_zero), ('reverse', reverse_zero),
                         ('copy', copy_zero), ('pop', pop_zero)]:
    report(label + ' empty', [], operation)
    report(label + ' singleton', [10], operation)
    report(label + ' pair', [10, 20], operation)

# 浅copy保留元素引用，结果容器必须新建；直接与saved调用都保留别名观察。
for saved in [False, True]:
    child = [10]
    values = [child, child]
    if saved:
        method = values.copy
        result = method(**{})
    else:
        result = values.copy()
    print('shallow copy', saved, result is values, result[0] is child,
          result[0] is result[1], values[0] is child)
    result.append('new tail')
    child.append(20)
    print('shallow copy mutated', saved, values, result)

# 零到三个位置参数覆盖数量边界；saved路径另外验证空关键字映射不误拒绝。
for name in ['clear', 'reverse', 'copy', 'pop']:
    for args in [(), (0,), (0, 1), (0, 1, 2)]:
        values = [10, 20]
        method = getattr(values, name)
        try:
            result = method(*args, **{})
            print('saved arity', name, args, 'returned', result, values)
        except Exception as error:
            print('saved arity', name, args, type(error).__name__, str(error), values)

# 有效pop仅测试现有小整数/bool与普通越界，不混入宽long或自定义__index__。
for index in [-3, -2, -1, 0, 1, 2, False, True]:
    values = [10, 20]
    try:
        result = values.pop(index)
        print('small index', index, 'returned', result, values)
    except Exception as error:
        print('small index', index, type(error).__name__, str(error), values)


def clear_three(values):
    return values.clear(touch('arg0', 0), touch('arg1', 1), touch('arg2', 2))


def reverse_three(values):
    return values.reverse(touch('arg0', 0), touch('arg1', 1), touch('arg2', 2))


def copy_three(values):
    return values.copy(touch('arg0', 0), touch('arg1', 1), touch('arg2', 2))


def pop_three(values):
    return values.pop(touch('arg0', 0), touch('arg1', 1), touch('arg2', 2))


for label, operation in [('clear three', clear_three), ('reverse three', reverse_three),
                         ('copy three', copy_three), ('pop three', pop_three)]:
    report(label, [10, 20], operation)


def clear_expanded(values):
    return values.clear(touch('arg0', 0), *[touch('star1', 1), touch('star2', 2)],
                        bad=touch('keyword', 3), **{'other': touch('mapping', 4)})


def reverse_expanded(values):
    return values.reverse(touch('arg0', 0), *[touch('star1', 1), touch('star2', 2)],
                          bad=touch('keyword', 3), **{'other': touch('mapping', 4)})


def copy_expanded(values):
    return values.copy(touch('arg0', 0), *[touch('star1', 1), touch('star2', 2)],
                       bad=touch('keyword', 3), **{'other': touch('mapping', 4)})


def pop_expanded(values):
    return values.pop(touch('arg0', 0), *[touch('star1', 1), touch('star2', 2)],
                      bad=touch('keyword', 3), **{'other': touch('mapping', 4)})


for label, operation in [('clear expanded', clear_expanded), ('reverse expanded', reverse_expanded),
                         ('copy expanded', copy_expanded), ('pop expanded', pop_expanded)]:
    report(label, [10, 20], operation)


class FailedExpansion:
    def __iter__(self):
        events.append('iter failed before binding')
        raise ValueError('star expansion failed')


def failed_expansion(values):
    return values.clear(*FailedExpansion())


report('star failure wins', [10, 20], failed_expansion)


class Holder:
    def __init__(self, value):
        self.value = value


def paused_clear(holder):
    return holder.value.clear((yield 'clear paused'))


def paused_reverse(holder):
    return holder.value.reverse((yield 'reverse paused'))


def paused_copy(holder):
    return holder.value.copy((yield 'copy paused'))


def paused_pop(holder):
    return holder.value.pop((yield 'pop paused'))


# 三个无参方法恢复后才绑定失败；合法pop继续使用暂停前捕获的原list。
for name, operation in [('clear', paused_clear), ('reverse', paused_reverse),
                        ('copy', paused_copy), ('pop', paused_pop)]:
    first = [10, 20]
    second = [30, 40]
    holder = Holder(first)
    iterator = operation(holder)
    print('pause', name, next(iterator), first, second)
    holder.value = second
    try:
        iterator.send(0)
    except StopIteration as done:
        print('resume', name, 'returned', done.value, first, second)
    except Exception as error:
        print('resume', name, type(error).__name__, str(error), first, second)

# 参数求值先于绑定；回调新建异常只抛一次，helper不得catch/repackage或替换它。
callback_errors = []


def raise_argument():
    events.append('argument callback')
    error = LookupError('argument callback failed')
    callback_errors.append(error)
    raise error


values = [10, 20]
events.clear()
try:
    values.pop(raise_argument(), 1)
except LookupError as error:
    print('argument callback', type(error).__name__, str(error), error is callback_errors[0],
          error.__context__ is None, values, events)
print('SUPPLEMENT_COMPLETED')
