# 未执行的跨模块应用；函数对象身份/元数据与generator正常、close、throw回归。
import origin_provider as provider
from origin_provider import exported as imported_alias
entry_name = __name__
print('imported', imported_alias.__module__, imported_alias is provider.exported)
provider.set_name('provider-renamed')
__name__ = 'caller-renamed'
first = provider.make_closure(1)
provider.__name__ = 'provider-external-name'
second = provider.make_closure(2)
print('nested', first.__module__, second.__module__, first is not second, first(), second())
print('old-import', imported_alias.__module__, imported_alias())

def replacement(value=20):
    return ['replacement', value]
provider.exported = replacement
print('replace', provider.exported.__module__, imported_alias.__module__, provider.exported(), imported_alias())
def select_old(function):
    return imported_alias
@select_old
def decorated(value=99):
    return value
print('return-old-decorator', decorated is imported_alias, decorated.__module__, decorated())
instance = provider.Owner()
print('method', provider.Owner.method.__module__, instance.method.__module__, instance.method())

stream = provider.make_stream(30)
other = provider.make_stream(40)
print('generator-functions', stream.__module__, other.__module__, stream is not other)
iterator = stream()
other_iterator = other()
print('first-yield', next(iterator), next(other_iterator))
stream.__module__ = 'changed-while-suspended'
provider.set_name('provider-after-yield')
__name__ = 'caller-after-yield'
print('second-yield', next(iterator), next(other_iterator))
print('exhausted', next(iterator, 'end'))
other_iterator.close()
throwing = stream()
print('before-throw', next(throwing))
try:
    throwing.throw(ValueError('injected'))
except ValueError as error:
    print('throw', str(error), next(throwing, 'closed'))
__name__ = entry_name
