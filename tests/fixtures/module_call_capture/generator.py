import provider


def change(value):
    print('argument', value)
    provider.f = provider.new
    return value


def resumed(module):
    yield 'before-call'
    yield module.f(change('after-resume'))
    yield module.f('second-call')


def suspended_argument(module):
    yield module.f(change((yield 'argument-pause')))
    yield module.f('after-suspended-call')


provider.f = provider.old
iterator = resumed(provider)
print('first-yield', next(iterator))
print('first-result', next(iterator))
print('second-result', next(iterator))
try:
    next(iterator)
except StopIteration:
    print('resumed-done')

provider.f = provider.old
iterator = suspended_argument(provider)
print('pause', next(iterator))
provider.f = provider.new
print('saved-target', next(iterator))
print('latest-target', next(iterator))
try:
    next(iterator)
except StopIteration:
    print('suspended-done')
