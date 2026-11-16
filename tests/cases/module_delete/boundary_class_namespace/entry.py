# ryupy: pending
# class namespace 删除与模块删除不同：读可回退外部全局，删除只查类命名空间。
token = ['global']
class Subject:
    token = ['class']
    saved = token
    del token
    after = token

assert Subject.saved == ['class']
assert Subject.after is token and 'token' not in Subject.__dict__
events = []
try:
    class Broken:
        events.append('before')
        del token
        events.append('after')
except NameError as error:
    assert str(error) == "name 'token' is not defined"
    print('class deletion missing local', type(error).__name__, str(error))
else:
    assert False
assert events == ['before'] and token == ['global']
Subject.token = ['restored']
assert Subject.token == ['restored'] and Subject.after is token
print('class fallback and error phase', events)
