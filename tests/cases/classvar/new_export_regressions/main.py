# ryupy: pending
# ClassVar 新增为真实导出后，旧 Optional/Union、TypeVar bound 与 NamedTuple 冷路径不能整体 NI。
import typing as t
assert repr(t.ClassVar) == 'typing.ClassVar'
T = t.TypeVar('Bound', bound=int)
assert T.__bound__ is int
assert T.__constraints__ == ()
U = t.TypeVar('Choices', int, str)
assert U.__constraints__ == (int, str)
special_choices = t.TypeVar('SpecialChoices', t.ClassVar, str)
assert special_choices.__constraints__ == (t.ClassVar, str)
pair = t.NamedTuple('Pair', [('number', int), ('text', str)])
p = pair(3, 'three')
assert p.number == 3 and p.text == 'three'
assert pair.__annotations__ == {'number': int, 'text': str}
key = t.TypeVar('Cold')
assert t.Optional[key] is t.Union[key, type(None)]
assert t.Union[int, str].__args__ == (int, str)
assert t.List[int].__args__ == (int,)
assert list[t.ClassVar[int]].__args__ == (t.ClassVar[int],)
print('new-export-old-cold-paths', T.__bound__, U.__constraints__, p)
