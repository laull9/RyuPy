# 文本与数据处理：表格对齐与表头。
from tabulate import tabulate

rows = [["a", 1], ["b", 22]]
print(tabulate(rows, headers=["name", "value"], tablefmt="github"))
print(tabulate([], headers=["x"]))
