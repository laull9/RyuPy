# 文本与数据处理：texttable 的列对齐。
from texttable import Texttable

table = Texttable()
table.set_cols_align(["l", "r"])
table.add_rows([["name", "n"], ["a", 1]])
print(table.draw())
