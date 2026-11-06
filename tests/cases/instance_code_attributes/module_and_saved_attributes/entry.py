# 只断言本例覆盖的通用属性/已保存callable；不将其通过算作点调用顺序已修复。
import subject

assert subject.regex_results == ('source', '123')
assert subject.labels == 'compile exec eval'
assert subject.saved_results == (('original', 'compiled', 'input'), ('original', 'output', ()), ('original', 12))
assert subject.constant_results == (('other', 'compiled', 'next'), ('other', 'sent', ()), ('other', 14))
assert subject.keyword_result == ('original', 'options', (('compile', 1), ('exec', 2), ('eval', 3)))
assert subject.saved_data == (['source', 'checked'], ['action'], ['value', 'read'])
assert subject.updated_data == (['source', 'checked'], ['new action'], ['value', 'read'])
assert subject.saved_data[0] is subject.updated_data[0]
assert subject.saved_data[1] is not subject.updated_data[1]
assert subject.saved_data[2] is subject.updated_data[2]
assert subject.changed_result == ('other', 'replacement', 'changed')
assert subject.retained_result == ('original', 'compiled', 'retained')
assert subject.restored_result == ('other', 'compiled', 'restored')
assert subject.replacement_result == ('other', 16)
assert subject.retained_exec_result == ('original', 'retained output', ())
assert subject.receiver_results == (('other', 'compiled', 'receiver'), ('other', 18))
assert subject.selection_events == ('selected',)
assert subject.events == ['selected']
assert subject.selected_result == ('other', 'selected output', (('compile', 4), ('exec', 5), ('eval', 6)))
print('instance module and saved code-named attributes')
