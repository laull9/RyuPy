# 入口：把多个包内模块串成一个数据处理流程。
from pipeline import runner, store
from pipeline.model import Record

store.add(Record("plain", 2))
try:
    print(runner.run([1, 2, 3, 4]))
except ValueError as error:
    print("recovered", error)
print(runner.run([3, 4]))
print(store.total(2), len(store.items))
for item in store.items:
    print(item.describe())
print(store.items[0].scaled(3))
store.clear()
print(len(store.items), store.total(1))
