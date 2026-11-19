# set.clear 求值/失败不变性及活迭代状态；原异常、成员和事件逐字节核对。
# 同 size clear/refill 的 CP 正例独立 pending，不让快照伪装成功。
events = []


def touch(label, value):
    events.append(label)
    return value


def report(label, operation, scope='active'):
    values = {10, 20}
    alias = values
    events.clear()
    try:
        result = operation(values)
        print(scope, label, 'returned', result, sorted(values),
              alias is values, sorted(alias), events)
    except Exception as error:
        print(scope, label, type(error).__name__, str(error), sorted(values),
              alias is values, sorted(alias), events)

# 参数求值异常保留原异常身份；参数自身的修改不得被失败调用撤销或追加 clear。
callback_error = LookupError('argument failed')


def fail_argument(values):
    events.append('argument mutates')
    values.add(30)
    raise callback_error


for saved in [False, True]:
    values = {10, 20}
    events.clear()
    try:
        if saved:
            method = values.clear
            method(fail_argument(values), bad=touch('unreached keyword', 2))
        else:
            values.clear(fail_argument(values), bad=touch('unreached keyword', 2))
    except Exception as error:
        print('active argument failure', saved, type(error).__name__, str(error),
              error is callback_error, sorted(values), events)


def mutate(values):
    events.append('argument mutates')
    values.add(30)
    return 0


def mutation_then_binding_error(values):
    return values.clear(mutate(values), bad=touch('keyword', 2))


report('argument mutation binding error', mutation_then_binding_error)

# 唯一 star 的展开异常与 keyword 求值顺序由 CP 原流决定，不猜优先级。
class BrokenStar:
    def __iter__(self):
        events.append('star iter')
        raise LookupError('star failed')


def broken_star(values):
    return values.clear(*touch('star expression', BrokenStar()),
                        bad=touch('keyword', 2))

report('star expansion failure', broken_star)

# 接收者先求值；参数重绑名称不改变原接收者，保存方法也不跟随名称重绑。
first = {10, 20}
second = {30, 40}
target = first


def receiver():
    events.append('receiver')
    return target


def rebind():
    global target
    events.append('argument rebinds')
    target = second
    return 0


events.clear()
try:
    receiver().clear(rebind())
except Exception as error:
    print('active receiver order', type(error).__name__, str(error),
          sorted(first), sorted(second), target is second, events)
method = first.clear
first_alias = first
first = second
print('active saved receiver', method(), sorted(first_alias), sorted(first),
      first is second, first_alias is second)
print('active repeated empty', method(), method(), sorted(first_alias))

# 每次 next 独立打印，保留错误重复、修复 size 后错误是否粘住、耗尽是否粘住。
def step(label, iterator, scope='active'):
    try:
        value = next(iterator)
        print(scope, label, 'returned', value)
    except Exception as error:
        print(scope, label, type(error).__name__, str(error))


values = {1, 2}
iterator = iter(values)
print('active untouched clear', values.clear())
step('untouched after clear', iterator)
step('untouched repeated error', iterator)
values.add(1)
values.add(2)
step('untouched size restored after error', iterator)

values = {1, 2}
iterator = iter(values)
step('partial before clear', iterator)
print('active partial clear', values.clear())
step('partial after clear', iterator)
values.add(1)
values.add(2)
step('partial size restored after error', iterator)

values = {1}
iterator = iter(values)
step('last value consumed', iterator)
values.clear()
step('last consumed but not exhausted', iterator)
step('last consumed repeated error', iterator)

values = {1}
iterator = iter(values)
step('exhaust before value', iterator)
step('exhaust confirmed', iterator)
values.clear()
values.add(2)
step('exhaust after clear and add', iterator)
step('exhaust repeated', iterator)

values = set()
iterator = iter(values)
print('active empty clear', values.clear())
step('empty after clear', iterator)
values.add(1)
step('empty exhausted after add', iterator)
values = set()
iterator = iter(values)
values.clear()
values.add(1)
step('empty unprobed clear and add', iterator)
step('empty unprobed repeated error', iterator)

# 失败的 clear 不改变集合/活迭代器；原成员继续读出，不把失败调用算成一次 clear。
values = {1}
iterator = iter(values)
try:
    values.clear(0)
except Exception as error:
    print('active failed clear iterator', type(error).__name__, str(error), sorted(values))
step('failed clear still live', iterator)
step('failed clear now exhausted', iterator)
