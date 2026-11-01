# 新原生容器的直接递归相等性抛Python异常，不让CLR栈溢出终止进程。
from collections import Counter, defaultdict, deque

first = deque()
second = deque()
first.append(first)
second.append(second)
for attempt in range(2):
    try:
        print(first == second)
    except RecursionError:
        print("recursive deque")
print(first == first, deque([1]) == deque([1]))

first = Counter()
second = Counter()
first["self"] = first
second["self"] = second
try:
    print(first == second)
except RecursionError:
    print("recursive counter")
print(Counter(a=1) == Counter(a=1))

first = defaultdict(None)
second = defaultdict(None)
first["self"] = first
second["self"] = second
try:
    print(first == second)
except RecursionError:
    print("recursive defaultdict")
print(defaultdict(None, a=1) == defaultdict(None, a=1))
