# 入口只观察真实实例模块，不在主体加入当前仍不支持的 except handler。
import subject

assert subject.text == 'compile exec eval globals locals super __class__'
assert subject.alias_result == ('first', 3, ())
assert subject.keyword_result == ('first', 7, (('compile', 8), ('exec', 9), ('eval', 10)))
assert subject.method_results == (('exec', 'first', 4), ('eval', 'first', 5))
assert subject.rebound_result == ('eval', 'second', 6)
assert subject.stored_value == ['first', 'second']
assert subject.stored_value is subject.box.eval
assert subject.regex_result == 'aaa' and subject.regex_alias_result == 'bbb'
assert subject.shadow_result == (('second', 11, ()), ('second', 12, ()))
assert subject.changed_result == ('replacement', 'second', 13)
assert subject.saved_after_change == ('first', 14, ())
assert subject.restored_result == ('second', 15, ())
assert subject.instance_result == 17
assert subject.ordered_result == 21
assert subject.ordered_events == ('receiver', 'property', 'argument', ('callback', 20))

try:
    subject.missing_attribute()
except AttributeError as error:
    assert str(error) == "'list' object has no attribute 'compile'"
else:
    assert False

subject.events.clear()
try:
    subject.failed_call()
except ValueError as error:
    assert str(error) == 'receiver failed'
else:
    assert False
assert subject.events == ['failed receiver']

subject.events.clear()
try:
    subject.failed_lookup()
except AttributeError as error:
    assert str(error) == "'list' object has no attribute 'exec'"
else:
    assert False
assert subject.events == []
print('instance ordinary code-named attributes, aliases, keywords and order')
