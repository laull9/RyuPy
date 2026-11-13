# assert 仍执行；消息只在失败时求值，失败不会吞掉消息副作用。
events = []
def condition(value):
    events.append(('condition', value))
    return value
def message():
    events.append('message')
    return 'failed'
assert condition(True), message()
print('success', events)
try:
    assert condition(False), message()
except AssertionError as e:
    print('failure', str(e), events)
print('debug', __debug__)
