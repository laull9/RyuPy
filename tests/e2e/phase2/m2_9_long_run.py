# M2.9：容器扩容、模块缓存与重复导入的长期运行检查。
import fixtures.module_case.state as first
import fixtures.module_case.state as second

items = []
for index in range(20000):
    items.append(index)
print(len(items), items[0], items[-1], sum(items))

lookup = {}
for index in range(5000):
    lookup[index] = index * 2
for index in range(0, 5000, 2):
    del lookup[index]
print(len(lookup), lookup[1], lookup[4999])

seen = set()
for index in range(5000):
    seen.add(index % 1000)
print(len(seen), 999 in seen)

print(first is second, first.value)
for _ in range(1000):
    import fixtures.module_case.state as current
    current.value += 1
print(first is current, first.value)
