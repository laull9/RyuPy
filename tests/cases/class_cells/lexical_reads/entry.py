import subject
original = subject.saved_class
obj = original()
assert original.before == 'module value'
assert original.seen == 'namespace value'
assert original.body_lambda() is original
assert obj.ident() is original
assert obj.nested()() is original
assert obj.ordinary() == 'module x'
assert obj.local()() == 'local value'
assert obj.parameter('parameter value')() == 'parameter value'
assert obj.global_value() == 'module value'
assert original.static() is original
assert original.class_method() == (original, original)
assert obj.property_value is original
assert subject.saved_method(None) is original
assert subject.Other().transplanted() is original
print('lexical class cell, namespace, shadowing and transplant')
