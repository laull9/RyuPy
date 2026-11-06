# 现有字符串方法闭环：普通覆盖、根描述符、保存方法及raw文本参数。
events = []
class Text(str):
    def __str__(self):
        events.append('str')
        return 'converted'
class Other(str):
    pass
value = Text(' Ab\nCd ')
assert type(value.strip()) is str and value.strip() == 'Ab\nCd'
assert value.lstrip() == 'Ab\nCd ' and value.rstrip() == ' Ab\nCd'
assert value.upper() == ' AB\nCD ' and value.lower() == ' ab\ncd '
assert value.splitlines() == [' Ab', 'Cd ']
assert value.splitlines(True) == [' Ab\n', 'Cd ']
assert value.split() == ['Ab', 'Cd']
assert Text('a,b').split(Text(',')) == ['a', 'b']
assert Text('-').join([Text('a'), Other('b')]) == 'a-b'
assert type(Text('-').join([Text('a')])) is str
assert ','.join([Text('a'), Other('b')]) == 'a,b'
assert Text('abcabc').replace(Text('ab'), Other('X'), 1) == 'Xcabc'
assert Text('abcabc').find(Text('b')) == 1
assert Text('abcabc').rfind(Text('b')) == 4
assert Text('abcabc').index(Text('b')) == 1
assert Text('abcabc').rindex(Text('b')) == 4
assert Text('abcabc').count(Text('ab')) == 2
assert Text('abc').startswith(Text('a')) and Text('abc').endswith(Text('c'))
assert Text('abc').encode(Text('ascii')) == b'abc'
assert events == []
print('methods', value.splitlines(), Text('-').join([Text('a'), Other('b')]))

class Custom(str):
    def splitlines(self):
        return ['custom']
class Child(Custom):
    def root_lines(self):
        return super().splitlines()
item = Custom('a\nb')
assert item.splitlines() == ['custom']
assert str.splitlines(item) == ['a', 'b']
assert Child('a\nb').root_lines() == ['custom']
class Direct(str):
    def lines(self):
        return super().splitlines()
assert Direct('a\nb').lines() == ['a', 'b']
saved = str.splitlines
assert saved(item) == ['a', 'b']
plain = Other('a\nb')
bound = plain.splitlines
assert bound.__self__ is plain

def changed(self):
    return ['changed']
for ignored in range(3):
    assert plain.splitlines() == ['a', 'b']
Other.splitlines = changed
assert plain.splitlines() == ['changed']
assert bound() == ['a', 'b']
assert str.splitlines(plain) == ['a', 'b']
for candidate in [item, plain, item, plain]:
    assert candidate.splitlines() in [['custom'], ['changed']]
print('binding', bound(), plain.splitlines(), item.splitlines())

class PropertyText(str):
    @property
    def splitlines(self):
        return lambda: ['property']
prop = PropertyText('a\nb')
assert prop.splitlines() == ['property']
assert str.splitlines(prop) == ['a', 'b']
item.splitlines = lambda: ['instance']
assert item.splitlines() == ['instance']
assert str.splitlines(item) == ['a', 'b']
assert str.__str__(value) == ' Ab\nCd ' and events == []
assert str(value) == 'converted' and events == ['str']
print('override', prop.splitlines(), item.splitlines(), events)

# 默认值协议仍保留真实子类身份；产生的新文本为根str，不调用__str__。
events.clear()
sequence = Text('a😀b')
assert len(sequence) == 3 and bool(sequence) and not bool(Text(''))
assert list(sequence) == ['a', '😀', 'b']
assert sequence[1] == '😀' and sequence[-1] == 'b'
assert sequence[:2] == 'a😀' and type(sequence[:2]) is str
assert Text('😀') in sequence and sequence in 'xa😀by'
assert sequence + Other('!') == 'a😀b!' and '!' + sequence == '!a😀b'
assert type(sequence + '') is str
assert Text('ab') * 2 == 'abab' and 2 * Text('ab') == 'abab'
assert Text('ab') * -1 == ''
first = Text('equal')
second = Other('equal')
assert first == 'equal' and 'equal' == first and first == second
assert hash(first) == hash('equal') == hash(second)
assert Text('a') < Other('b') and 'a' < Other('b')
mapping = {first: 1}
mapping['equal'] = 2
mapping[second] = 3
assert len(mapping) == 1 and mapping['equal'] == 3
assert list(mapping)[0] is first
assert len({first, second, 'equal'}) == 1
assert events == []
print('protocols', len(sequence), list(sequence), mapping['equal'])
