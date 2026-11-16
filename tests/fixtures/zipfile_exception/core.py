from zipfile import BadZipFile as Bad
import zipfile as archive

assert Bad is archive.BadZipFile
assert Bad is not Exception
assert Bad.__name__ == 'BadZipFile'
assert Bad.__module__ == 'zipfile'
assert repr(Bad) == "<class 'zipfile.BadZipFile'>"
assert callable(Bad)
assert issubclass(Bad, Exception)
assert not issubclass(Bad, ValueError)
assert not issubclass(Bad, OSError)
assert Bad().args == ()
assert Bad('').args == ('',)
token = []
e = Bad(token, 3)
assert e.args[0] is token
assert e.args[1] == 3
assert type(e) is Bad
assert isinstance(e, Bad)
assert isinstance(e, Exception)
assert not isinstance(e, ValueError)
assert str(Bad('bad')) == 'bad'
assert repr(Bad('bad')) == "BadZipFile('bad')"
try:
    Bad(message='bad')
except TypeError:
    print('keyword rejected')
else:
    raise AssertionError('keyword accepted')
try:
    raise Bad
except archive.BadZipFile as error:
    assert error.args == ()
    print('class caught')

calls = []
class Message:
    def __str__(self):
        calls.append('str')
        return 'message'
class Child(Bad):
    pass
class Grandchild(Child):
    pass
message = Message()
error = Grandchild(message)
assert calls == []
assert type(error) is Grandchild
assert isinstance(error, Child)
assert isinstance(error, Bad)
assert isinstance(error, Exception)
assert error.args[0] is message
assert str(error) == 'message'
assert calls == ['str']
try:
    raise error
except ValueError:
    raise AssertionError('wrong CLR parent')
except archive.BadZipFile as caught:
    assert caught is error
    print('subclass caught')
print('badzip identity and args')
