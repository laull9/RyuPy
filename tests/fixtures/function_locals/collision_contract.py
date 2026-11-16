# 普通locals快照的碰撞键、副作用、声明名与失败调用同步边界。

EVENTS = []

class Collision:
    def __init__(self, mode):
        self.mode = mode
    def __hash__(self):
        return hash('value')
    def __eq__(self, other):
        if other == 'value':
            EVENTS.append(self.mode)
            if self.mode == 'key-error':
                raise KeyError('collision-key')
            if self.mode == 'value-error':
                raise ValueError('collision-value')
            return True
        return False

def collisions():
    value = 1
    snapshot = locals()
    del snapshot['value']
    key = Collision('update')
    snapshot[key] = 2
    value = 3
    assert locals() is snapshot
    assert snapshot[key] == 3
    assert list(snapshot)[0] is key
    assert EVENTS == ['update']
    EVENTS.clear()
    del value
    key.mode = 'delete'
    assert locals() is snapshot
    assert key not in snapshot
    assert EVENTS == ['delete']
    print('collision.update-delete', EVENTS)
    EVENTS.clear()
    snapshot[key] = 4
    key.mode = 'key-error'
    assert locals() is snapshot
    assert snapshot[key] == 4
    assert EVENTS == ['key-error']
    print('collision.key-error', EVENTS)
    EVENTS.clear()
    key.mode = 'value-error'
    after_failure = 17
    try:
        locals()
    except ValueError as error:
        assert str(error) == 'collision-value'
        assert 'after_failure' not in snapshot
        assert 'error' not in snapshot
        assert EVENTS == ['value-error']
        print('collision.value-error', str(error), EVENTS)
    else:
        raise AssertionError('ValueError must escape')

collisions()

def declarations():
    annotation: int
    snapshot = locals()
    snapshot['annotation'] = 7
    snapshot['unreachable'] = 8
    assert locals() is snapshot
    assert snapshot['annotation'] == 7
    assert 'unreachable' not in snapshot
    print('collision.declarations', list(snapshot), snapshot['annotation'])
    return snapshot
    unreachable = 9

declarations()

def invalid_no_refresh():
    value = 'old'
    snapshot = locals()
    value = 'new'
    try:
        locals(1)
    except TypeError:
        assert snapshot['value'] == 'old'
        assert 'snapshot' not in snapshot
        print('collision.invalid-no-refresh', snapshot['value'])
    assert locals() is snapshot
    assert snapshot['value'] == 'new'
    print('collision.valid-refresh', snapshot['value'])

invalid_no_refresh()
