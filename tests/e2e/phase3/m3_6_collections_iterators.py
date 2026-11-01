# 容器工具保持引用身份、缺键工厂和惰性消费，不用物化模拟无限迭代器。
from collections import Counter, defaultdict, deque
from itertools import chain, islice, repeat

value = []
items = list(repeat(value, 3))
print(items[0] is items[2], list(repeat(1, -2)))
print(list(islice(repeat(2), 4)))
source = iter(range(10))
print(list(islice(source, 5, 2)), next(source))
source = iter(range(10))
print(list(islice(source, 1, 6, 3)), next(source))
print(list(chain.from_iterable([[1], [], [2, 3]])))

class Source:
    def __iter__(self):
        print("iter")
        return iter([1, 2])

joined = chain(Source())
print("created chain")
print(next(joined), list(joined))
empty = islice(Source(), 0)
print("created slice", list(empty))
for arguments in [(-1,), (None, None, 0)]:
    try:
        islice([], *arguments)
    except ValueError:
        print("slice rejected")

queue = deque([1, 2], maxlen=3)
queue.appendleft(0)
queue.append(3)
print(list(queue), queue.maxlen, queue.pop(), queue.popleft())
queue.extendleft([4, 5])
queue.rotate(1)
print(list(queue), len(queue), bool(queue), queue[0])
iterator = iter(queue)
queue.append(6)
try:
    next(iterator)
except RuntimeError:
    print("deque changed")

calls = []
def factory():
    calls.append(1)
    return []
groups = defaultdict(factory)
print(groups.get("x"), "x" in groups, calls)
groups["x"].append(2)
print(dict(groups), calls, groups["x"] is groups["x"])
print(isinstance(groups, dict), issubclass(defaultdict, dict))
counts = Counter("abaca")
print(counts["a"], counts["missing"], "missing" in counts)
counts.update("bc")
counts.subtract({"a": 4})
print(dict(counts), counts.most_common(), list(counts.elements()), counts.total())
print(Counter(a=1) == Counter(a=1, b=0))

# 增量运算保留对象别名，允许普通映射，并保留异常前已写回的计数。
counts = Counter(a=1)
alias = counts
counts += {"a": 2, "b": 3}
print(counts is alias, dict(counts))
try:
    counts += {"a": 1, "b": "bad"}
except TypeError:
    print(dict(counts))
counts -= {"a": 5}
print(dict(counts), counts is alias)
queue = deque([1, 2, 3])
queue.insert(1, 9)
print(list(queue), queue.index(9), queue.count(2))
del queue[-1]
print(list(queue), list(reversed(queue)))
queue.reverse()
print(list(queue), repr(queue.copy()))
try:
    hash(queue)
except TypeError:
    print("deque unhashable")
print(repr(defaultdict(None, x=1)))

# chain 的外层失败会耗尽，但已建立内层的 next 异常不关闭内层。
joined = chain([1], 42, [3])
print(next(joined))
try:
    next(joined)
except TypeError:
    print("bad child")
print(next(joined, "done"), next(joined, "done"))
class Failing:
    def __iter__(self):
        return self
    def __next__(self):
        print("advance")
        raise ValueError("fail")
for joined in [chain.from_iterable(Failing()), chain(Failing())]:
    for attempt in range(2):
        try:
            print(next(joined, "done"))
        except ValueError:
            print("failed")
print(repr(repeat([], -1)))
queue = deque([1, 2])
backwards = reversed(queue)
queue.append(3)
try:
    next(backwards)
except RuntimeError:
    print("reverse deque changed")

# 空 mapping 更新只是复制；非空 Counter.update 是“新计数 + 旧计数”。
text_counts = Counter({"x": "a"})
print(dict(text_counts))
text_counts.update({"x": "b"})
print(dict(text_counts))
class Count:
    def __init__(self, label):
        self.label = label
    def __add__(self, other):
        print("count add", self.label, other.label)
        return 7
old = Count("old")
new = Count("new")
custom = Counter({"x": old})
print(custom["x"] is old)
custom.update({"x": new})
print(dict(custom))
for queue in [deque(), deque([1]), deque(maxlen=0)]:
    cursor = iter(queue)
    queue.rotate(0)
    print(next(cursor, "done"))
queue = deque(maxlen=0)
cursor = iter(queue)
queue.extend([1, 2])
queue.clear()
print(next(cursor, "done"))
print(dict(Counter(a=1) | Counter(a=1.0)))
counts = Counter(a=1)
cursor = iter(counts)
counts |= {"b": -1}
print(next(cursor))

# 映射 update 逐项写回，不预先耗尽来源生成器。
groups = defaultdict(list)
def entries():
    yield ("a", 1)
    print("partial mapping", dict(groups))
    raise ValueError("stop")
try:
    groups.update(entries())
except ValueError:
    print(dict(groups))
class DisplayCount:
    def __repr__(self):
        print("count repr")
        return "shown"
print(repr(Counter({"x": DisplayCount()})))

# 原生 dict 派生包装必须接通常见 JSON 与关键字展开，不触发缺键工厂。
import json
mapping = defaultdict(list, x=[1])
print(json.dumps(mapping, sort_keys=True))
print(json.dumps(Counter(a=2), sort_keys=True))
def keywords(**values):
    return values
print(keywords(**mapping), keywords(**Counter(a=2)))

# 聚合期间用户数值回调改变结构必须使字典迭代失效。
class ChangeTotal:
    def __radd__(self, other):
        totals["new"] = 1
        return 1
totals = Counter({"x": ChangeTotal()})
try:
    totals.total()
except RuntimeError:
    print("counter changed during total")
elements = Counter(a=1.5, b=2).elements()
try:
    next(elements)
except TypeError:
    print("invalid element count")
print(next(elements, "done"))
counts = Counter(a=2)
elements = counts.elements()
print(next(elements))
counts["b"] = 1
print(next(elements))
try:
    next(elements)
except RuntimeError:
    print("elements changed")
print(next(elements, "done"))
print(Counter("aba").most_common(n=1))
