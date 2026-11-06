# 完整公开语义已通过固定CP与B3差分；保原源码行号。
# 完整应用：普通容器方法的两个声明与实际查询，保持声明原对象和最终方法绑定。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
class Index:
    def __init__(self):
        self.rows = ["alpha", "beta", "gamma"]
        self.names = {"first": 0, "second": 1}

    @overload
    def find(self, key: int) -> str:
        ...

    @overload
    def find(self, key: str) -> str:
        ...

    def find(self, key):
        if isinstance(key, str):
            key = self.names[key]
        return self.rows[key]

index = Index()
print(index.find("first"), index.find(2))
functions = get_overloads(Index.find)
bound_functions = get_overloads(index.find)
print(len(functions), len(bound_functions), functions[0] is bound_functions[0])
print([function.__qualname__ for function in functions])
print(functions[0](index, 0), functions[1](index, "first"))
print(index.find.__func__ is Index.find)
print(index.find.__module__, index.find.__qualname__)
print(clear_overloads(), get_overloads(index.find))
