# 普通函数真实locals：身份、同步、顺序、清理与调用隔离。

class Token:
    pass

class Guard:
    def __enter__(self):
        return 'entered'
    def __exit__(self, kind, value, trace):
        return False

TOKEN = Token()

def inspect_state(z, a, /, b=3, *rest, key=4, **extras):
    snapshot = locals()
    assert list(snapshot) == ['z', 'a', 'b', 'key', 'rest', 'extras']
    assert snapshot['z'] is TOKEN
    assert 'later' not in snapshot
    assert 'snapshot' not in snapshot
    print('locals.parameters', list(snapshot), snapshot['rest'], snapshot['extras'])
    alias = snapshot
    snapshot['foreign'] = TOKEN
    snapshot['a'] = 'external'
    snapshot['later'] = 'not-bound'
    assert a == 2
    assert locals() is snapshot
    assert snapshot['a'] == 2
    assert 'later' not in snapshot
    assert snapshot['foreign'] is TOKEN
    assert snapshot['alias'] is snapshot
    later = 7
    assert locals() is alias
    assert alias['later'] == 7
    del later
    assert locals() is alias
    assert 'later' not in alias
    later = 9
    assert locals()['later'] == 9
    print('locals.synchronize', a, later, snapshot is alias, snapshot['foreign'] is TOKEN)
    try:
        raise ValueError('temporary')
    except ValueError as caught:
        assert locals()['caught'] is caught
    assert 'caught' not in locals()
    with Guard() as entered:
        assert locals()['entered'] == 'entered'
    # 合成异常、with状态和求值临时槽不得出现在源码locals中。
    assert list(locals()) == ['z', 'a', 'b', 'key', 'rest', 'extras', 'foreign', 'snapshot', 'alias', 'later', 'entered']
    print('locals.cleanup', list(snapshot))
    message = '%(a)s:%(b)s:%(key)s:%(later)s' % locals()
    assert message == '2:3:4:9'
    print('locals.mapping', message)
    return snapshot

first = inspect_state(TOKEN, 2, 3, 5, 6, key=4, extra=8)
second = inspect_state(TOKEN, 2, 3, 5, 6, key=4, extra=8)
assert first is not second
assert first['snapshot'] is first
assert second['snapshot'] is second
print('locals.invocations', first is second, first['z'] is second['z'])

def recursive(n):
    own = locals()
    if n:
        child = recursive(n - 1)
        assert own is not child
        assert own['n'] == n
        assert child['n'] == n - 1
    assert locals() is own
    return own

root = recursive(3)
assert root['child']['child']['child']['n'] == 0
print('locals.recursive', root['n'], root['child']['n'])
plain_lambda = lambda value: locals()
assert plain_lambda(TOKEN)['value'] is TOKEN
print('locals.lambda', list(plain_lambda(TOKEN)))
