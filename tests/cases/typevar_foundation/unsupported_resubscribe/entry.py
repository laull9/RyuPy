# ryupy: pending
# 别名再次订阅需要变量替换；本轮基础只记录合法未支持边界。
import typing as t
T = t.TypeVar('T')
assert t.List[T][int] == t.List[int]
assert t.Dict[str, T][int] == t.Dict[str, int]
print('resubscribe', True)
