import sys

if sys.argv[1] == 'en':
    import greetings.english as selected
    import greetings.english as repeated
else:
    import greetings.french as selected
    import greetings.french as repeated

print(selected.greet())
print(selected.load_text())
print(selected.source_file() == selected.__file__)
print(selected is repeated)

# 被导入模块本身没有__file__赋值；外部setter必须写回内部读取的同一槽。
saved_file = selected.__file__
saved_text = selected.load_text()
selected.__file__ = None
assert selected.__file__ is None
assert selected.source_file() is None
shared_token = []
selected.__file__ = shared_token
assert selected.__file__ is shared_token
assert selected.source_file() is shared_token
selected.__file__ = saved_file
assert selected.source_file() == saved_file
assert selected.load_text() == saved_text
