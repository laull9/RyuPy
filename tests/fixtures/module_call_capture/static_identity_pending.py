import provider
import provider as alias
import other
from provider import old as imported


def change(label):
    print('arg', label)
    provider.f = provider.new
    return label


def parameter(module):
    return module.f(change('parameter'))


def missing(module):
    try:
        module.absent(change('must-not-run'))
    except AttributeError:
        print('missing-before-arg')


def noncallable(module):
    try:
        module.f(change('noncallable'))
    except TypeError:
        print('noncallable-after-arg')


def rebind_receiver():
    global receiver
    print('arg', 'receiver')
    receiver = other
    return 11


def rebind_imported():
    global imported
    print('arg', 'from-import')
    imported = provider.new
    return 12


def rebind_chosen():
    global chosen
    print('arg', 'global-slot')
    chosen = provider.new
    return 13


class Argument:
    @property
    def value(self):
        print('property', 'argument')
        provider.f = provider.new
        return 14


def global_target(value):
    print('global-original', value)
    return 'global-result'


def rebind_global_target():
    global global_target
    print('arg', 'function-slot')
    global_target = provider.new
    return 15


def through_same_site(module, value):
    return module.f(value)


print('alias-identity', alias is provider)
provider.f = provider.old
print('direct', provider.f(change('direct')))
provider.f = provider.old
print('alias', alias.f(change('alias')))
provider.f = provider.old
print('parameter', parameter(alias))
missing(provider)
provider.f = 42
noncallable(provider)
provider.f = provider.old
receiver = provider
print('receiver', receiver.f(rebind_receiver()))
print('new-receiver', receiver is other)
print('from-import', imported(rebind_imported()))
print('new-imported', imported is provider.new)
chosen = provider.old
print('global-slot', chosen(rebind_chosen()))
print('new-chosen', chosen is provider.new)
provider.f = provider.old
print('nested', provider.f(provider.f(change('nested'))))
print('after-nested', provider.f())
for index in range(3):
    provider.f = provider.old
    print('site-old', through_same_site(provider, index))
    provider.f = provider.new
    print('site-new', through_same_site(provider, index))
    print('site-other', through_same_site(other, index))
provider.f = provider.old
print('arity0', provider.f())
provider.f = provider.old
print('arity2', provider.f(change('two'), 2))
provider.f = provider.old
print('arity3', provider.f(change('three'), 2, 3))
provider.f = provider.old
print('expanded', provider.f(*[change('star')], **{'b': 2, 'c': 3}))
provider.f = provider.old
print('property-argument', provider.f(Argument().value))
print('function-slot', global_target(rebind_global_target()))
print('new-function-slot', global_target is provider.new)
