# 参数绑定先于索引协议；索引回调改列表后按新内容执行，跨yield冻结原接收者。
events = []
callback_errors = []


class Index:
    def __init__(self, values, action, index):
        self.values = values
        self.action = action
        self.index = index

    def __index__(self):
        events.append('index')
        if self.action == 'clear':
            self.values.clear()
        elif self.action == 'append':
            self.values.append(30)
        elif self.action == 'replace':
            self.values.clear()
            self.values.append(40)
        elif self.action == 'raise':
            error = LookupError('index callback failed')
            callback_errors.append(error)
            raise error
        return self.index


def touch(label, value):
    events.append(label)
    return value


def too_many(values, index):
    return values.pop(touch('arg0', index), touch('arg1', 1))


def keyword(values, index):
    return values.pop(touch('arg0', index), bad=touch('keyword', 1))


def expanded(values, index):
    return values.pop(touch('arg0', index), *[touch('star', 1)],
                      bad=touch('keyword', 2), **{'other': touch('mapping', 3)})


for name, operation in [('arity', too_many), ('keyword', keyword), ('expanded', expanded)]:
    values = [10, 20]
    events.clear()
    try:
        operation(values, Index(values, 'clear', 0))
    except Exception as error:
        print(name, type(error).__name__, str(error), values, events)

for empty in [False, True]:
    for action, index in [('keep', 0), ('clear', 0), ('append', -1),
                          ('replace', 0), ('replace', -1), ('clear', 'bad'),
                          ('append', 4294967296), ('append', 'bad')]:
        values = [] if empty else [10, 20]
        events.clear()
        try:
            result = values.pop(Index(values, action, index))
            print('mutation', empty, action, 'returned', result, values, events)
        except Exception as error:
            print('mutation', empty, action, type(error).__name__, str(error), values, events)

# 用户回调抛出的对象和活动handler上下文必须保留，既不包装也不重复调用。
values = [10, 20]
events.clear()
try:
    raise ValueError('outer identity')
except ValueError as outer:
    try:
        values.pop(Index(values, 'raise', 0))
    except LookupError as error:
        print('identity', error is callback_errors[0], error.__context__ is outer,
              type(error).__name__, str(error), values, events)


class Holder:
    def __init__(self, value):
        self.value = value


def paused(holder):
    return holder.value.pop((yield 'paused'))


for index in [Index(None, 'keep', 0), Index(None, 'keep', 4294967296), Index(None, 'keep', 'bad')]:
    first = [10, 20]
    second = [30, 40]
    holder = Holder(first)
    iterator = paused(holder)
    events.clear()
    print('pause', next(iterator), first, second, events)
    holder.value = second
    try:
        iterator.send(index)
    except StopIteration as done:
        print('resume returned', done.value, first, second, events)
    except Exception as error:
        print('resume', type(error).__name__, str(error), first, second, events)
print('ORDER_YIELD_COMPLETED')
