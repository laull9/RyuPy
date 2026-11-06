# exact set.clear 参数绑定、身份与无 hash 清空；从唯一 CP 参考抽取有限通过域。
# 通用重复/非法关键字展开、任意 mapping 与根/子类仍为独立 pending。
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


def direct_empty(values):
    return values.clear()


def direct_one(values):
    return values.clear(touch('arg0', 0))


def direct_two(values):
    return values.clear(touch('arg0', 0), touch('arg1', 1))


def direct_keyword(values):
    return values.clear(bad=touch('keyword', 2))


def direct_mixed(values):
    return values.clear(touch('arg0', 0), bad=touch('keyword', 2))


def direct_empty_expansion(values):
    return values.clear(*touch('star', []), **touch('mapping', {}))


def direct_star(values):
    return values.clear(*touch('star', [0, 1]))


def direct_star_keyword(values):
    return values.clear(*touch('star', [0]), **touch('mapping', {'bad': 2}))


for label, operation in [('direct zero', direct_empty), ('direct one', direct_one),
                         ('direct two', direct_two), ('direct keyword', direct_keyword),
                         ('direct mixed', direct_mixed),
                         ('direct empty expansions', direct_empty_expansion),
                         ('direct star', direct_star),
                         ('direct star keyword', direct_star_keyword)]:
    report(label, operation)

# 保存方法必须固定原集合，失败不修改集合；空展开允许真正调用。
for mode in ['zero', 'one', 'two', 'keyword', 'mixed', 'empty expansions',
             'star', 'star keyword']:
    values = {10, 20}
    alias = values
    events.clear()
    method = values.clear
    try:
        if mode == 'zero':
            result = method()
        elif mode == 'one':
            result = method(touch('arg0', 0))
        elif mode == 'two':
            result = method(touch('arg0', 0), touch('arg1', 1))
        elif mode == 'keyword':
            result = method(bad=touch('keyword', 2))
        elif mode == 'mixed':
            result = method(touch('arg0', 0), bad=touch('keyword', 2))
        elif mode == 'empty expansions':
            result = method(*touch('star', []), **touch('mapping', {}))
        elif mode == 'star':
            result = method(*touch('star', [0, 1]))
        elif mode == 'star keyword':
            result = method(*touch('star', [0]), **touch('mapping', {'bad': 2}))
        print('active saved', mode, 'returned', result, sorted(values),
              alias is values, sorted(alias), events)
    except Exception as error:
        print('active saved', mode, type(error).__name__, str(error),
              sorted(values), alias is values, sorted(alias), events)

# 非空、空集合均原地清空；不依赖地址或 set 显示顺序。
for initial in [[], [1], [1, 2, 3]]:
    values = set(initial)
    alias = values
    method = values.clear
    result = method()
    print('active identity', initial, result is None, values is alias,
          len(values), len(alias), method() is None)

# 清空存量用户键不重新调用 hash/equality；强引用保留键，排除析构时机。
class Key:
    def __hash__(self):
        events.append('hash')
        if self.fail:
            raise LookupError('hash must not run during clear')
        return 17

    def __eq__(self, other):
        events.append('equal')
        raise LookupError('equality must not run during clear')


for saved in [False, True]:
    key = Key()
    key.fail = False
    values = {key}
    alias = values
    key.fail = True
    events.clear()
    try:
        if saved:
            method = values.clear
            result = method()
        else:
            result = values.clear()
        print('active user key no callbacks', saved, result is None,
              len(values), values is alias, events)
    except Exception as error:
        print('active user key no callbacks', saved, type(error).__name__,
              str(error), len(values), values is alias, events)
