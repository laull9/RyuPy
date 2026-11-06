# CPython 3.12.13 普通函数 locals 的原始语义探针；不导入或运行被测项目。
import sys
print('python', sys.version.split()[0])
def basic(a, /, b=2, *args, k=3, **kwargs):
    print('initial-order', list(locals()))
    d = locals()
    print('assignment-not-yet', 'd' in d)
    x = 1
    print('stale', 'x' in d)
    e = locals()
    print('same', d is e, 'd' in d, 'e' in d, d['x'])
    d['x'] = 99
    d['extra'] = 7
    del d['a']
    print('no-writeback', x, a)
    locals()
    print('refresh', d['x'], d['a'], d['extra'], list(d))
    del x
    d['x'] = 88
    locals()
    print('deleted', 'x' in d)
    print('unbound', 'later' in d)
    later = 9
    del d['b']
    locals()
    print('reinsert-order', list(d))
basic(1, 4, 5, k=6, z=7)
def exception_cleanup():
    d = locals()
    try:
        1 / 0
    except Exception as err:
        locals()
        print('except-bound', 'err' in d)
    print('except-stale', 'err' in d)
    locals()
    print('except-refreshed', 'err' in d)
exception_cleanup()
def object_alias():
    x = []
    d = locals()
    d['x'].append(1)
    print('object-alias', x, d['x'] is x)
    d['x'] = [2]
    print('replace-no-writeback', x)
    locals()
    print('replace-refreshed', d['x'] is x)
object_alias()
def recursive(n):
    d = locals()
    if n:
        child = recursive(n-1)
        print('recursive-distinct', d is child, d['n'], child['n'])
    return d
recursive(1)
EVENTS = []
class Key:
    def __hash__(self):
        EVENTS.append('hash')
        return hash('x')
    def __eq__(self, other):
        EVENTS.append(('eq', other))
        return other == 'x'
    def __repr__(self):
        return '<Key>'
def collision():
    d = locals()
    key = Key()
    d[key] = 'extra'
    EVENTS.clear()
    x = 3
    locals()
    print('collision-events', EVENTS)
    print('collision-keys', list(d), d[key], 'x' in d)
collision()
class ExplodingKey:
    def __hash__(self):
        return hash('x')
    def __eq__(self, other):
        EVENTS.append(('explode', other))
        raise ValueError('eq failed')
def collision_error():
    d = locals()
    d[ExplodingKey()] = 'extra'
    x = 2
    try:
        locals()
        print('collision-error', 'swallowed')
    except Exception as err:
        print('collision-error', type(err).__name__, str(err))
collision_error()
for name, expr in [
    ('pos', 'locals(1)'), ('kw', 'locals(x=1)'), ('both','locals(1,x=2)'),
    ('star0','locals(*[])'), ('star1','locals(*[1])'),
    ('dstar0','locals(**{})'), ('dstar1','locals(**{"x":1})'),
    ('nonstring','locals(**{1:2})'), ('duplicate','locals(x=1, **{"x":2})'),
    ('noniter','locals(*1)'), ('nonmap','locals(**1)')]:
    try:
        value = eval(expr)
        print('call', name, 'ok' if isinstance(value,dict) else value)
    except Exception as err:
        print('call', name, type(err).__name__, str(err))
class Args:
    def __iter__(self):
        print('star-iter')
        return iter([1])
    def __len__(self):
        print('star-len')
        return 1
def call_effects():
    try:
        locals(*Args(), x=print('kw-eval'))
    except Exception as err:
        print('call-effects-error', type(err).__name__, str(err))
call_effects()
def alias_scope(x):
    alias = locals
    print('alias-scope', sorted(alias()))
alias_scope(4)
def outer(x):
    def inner():
        print('closure-free', sorted(locals()), x)
    print('closure-cell', sorted(locals()))
    inner()
outer(5)
def comprehensions(x):
    print('comprehension', [sorted(locals()) for y in [1]])
comprehensions(2)
def generator(x):
    yield locals()
    x = 2
    yield locals()
g = generator(1)
gd = next(g)
print('generator-stale', gd['x'])
print('generator-same', next(g) is gd, gd['x'])
class Scope:
    a = 1
    print('class-scope', sorted(locals()))
print('module-scope', locals() is globals())
# 同步是逐槽更新，可见中间态；碰撞回调能改写已同步槽。
ACTIVE = None
CALLBACK = None
class CallbackKey:
    def __hash__(self):
        return hash('x')
    def __eq__(self, other):
        if other == 'x':
            CALLBACK()
            return True
        return False
    def __repr__(self):
        return '<CallbackKey>'
def observe_sync():
    EVENTS.append(('during', ACTIVE['a'], ACTIVE.get('z', 'absent')))
    ACTIVE['a'] = 999
    ACTIVE['z'] = 888
def partial_sync():
    global ACTIVE, CALLBACK
    a = 1
    d = locals()
    ACTIVE = d
    CALLBACK = observe_sync
    d[CallbackKey()] = 'external'
    a = 2
    x = 3
    z = 4
    EVENTS.clear()
    locals()
    print('partial-events', EVENTS)
    print('partial-result', d['a'], d['z'], a, z)
partial_sync()
# 重入 f_locals 属于明确边界：相同frame同步可以重入，内层看到逐步更新。
REENTERED = False
def reenter_sync():
    global REENTERED
    if not REENTERED:
        REENTERED = True
        frame = sys._getframe(2)
        nested = frame.f_locals
        EVENTS.append(('reentrant', nested is ACTIVE, nested['a'], nested.get('z','absent')))
def reentrant_sync():
    global ACTIVE, CALLBACK
    a = 1
    d = locals()
    ACTIVE = d
    CALLBACK = reenter_sync
    d[CallbackKey()] = 'external'
    a = 2
    x = 3
    z = 4
    EVENTS.clear()
    locals()
    print('reentrant-events', EVENTS)
    print('reentrant-final', d['a'], d['z'])
reentrant_sync()
# 异常后，较前变量已经刷新，较后变量尚未刷新，无事务回滚。
def interrupted_sync():
    a = 1
    d = locals()
    d[ExplodingKey()] = 'external'
    a = 2
    x = 3
    z = 4
    try:
        locals()
    except ValueError:
        print('interrupted-sync', d['a'], 'z' in d)
interrupted_sync()
# 尚未绑定的源码槽，不允许伪装成未知额外键而保留。
def unbound_spoof():
    d = locals()
    d['future'] = 1
    d['annotated'] = 2
    annotated: int
    locals()
    print('unbound-spoof', 'future' in d, 'annotated' in d)
    future = 3
unbound_spoof()
# 清空同一对象后刷新，仅恢复当时已绑定源码槽。
def clear_then_sync():
    x = 1
    d = locals()
    d['extra'] = 2
    d.clear()
    locals()
    print('clear-restore', list(d), d['d'] is d)
clear_then_sync()
# CPython 局部表由编译产物决定：纯注解未生成槽，但读操作/不可达赋值仍能创建槽。
def local_table():
    pure: int
    read: int
    if False:
        dead = 1
        print(read)
    d = locals()
    d['pure'] = 1
    d['read'] = 2
    d['dead'] = 3
    locals()
    print('local-table', 'pure' in d, 'read' in d, 'dead' in d)
print('local-table-varnames', local_table.__code__.co_varnames)
local_table()
def failed_call_no_sync():
    x = 1
    d = locals()
    x = 2
    try:
        locals(1)
    except TypeError:
        print('failed-call-no-sync', d['x'], 'd' in d)
failed_call_no_sync()
def evaluation_order():
    x = 1
    d = locals()
    x = 2
    try:
        locals(print('arg-before-check', d['x']), **{'a':print('keyword-before-check')})
    except TypeError as err:
        print('evaluated-call-error', str(err), d['x'])
evaluation_order()
# 同样语法名称可被参数/赋值遮蔽，不是 builtin 便不能应用特殊 lowering。
def shadow(locals):
    print('shadow', locals())
shadow(lambda: 'custom')
# 删除未绑定槽时 CPython 对 KeyError（包括用户 __eq__ 抛出）特殊压制。
DELETE_ERROR = None
class DeleteKey:
    def __hash__(self):
        return hash('unbound')
    def __eq__(self, other):
        EVENTS.append(('delete-eq', other))
        raise DELETE_ERROR('from equality')
    def __repr__(self):
        return '<DeleteKey>'
def unbound_delete_error():
    a = 1
    d = locals()
    d[DeleteKey()] = 9
    a = 2
    if False:
        unbound = 3
    z = 4
    EVENTS.clear()
    try:
        locals()
        print('delete-error', DELETE_ERROR.__name__, 'suppressed', d['a'], 'z' in d, EVENTS)
    except Exception as err:
        print('delete-error', DELETE_ERROR.__name__, type(err).__name__, str(err), d['a'], 'z' in d, EVENTS)
DELETE_ERROR = KeyError
unbound_delete_error()
DELETE_ERROR = ValueError
unbound_delete_error()
class EqualDeleteKey:
    def __hash__(self):
        return hash('unbound')
    def __eq__(self, other):
        EVENTS.append(('delete-ok-eq', other))
        return other == 'unbound'
    def __repr__(self):
        return '<EqualDeleteKey>'
def unbound_delete_success():
    d = locals()
    d[EqualDeleteKey()] = 9
    if False:
        unbound = 3
    EVENTS.clear()
    locals()
    print('delete-success', list(d), EVENTS)
unbound_delete_success()
