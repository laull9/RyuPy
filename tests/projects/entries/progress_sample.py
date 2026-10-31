# 终端与自动化：progress 的 Bar 构造与计数。
from progress.bar import Bar

bar = Bar("Working", max=3, suffix="%(percent)d%%")
print(bar.max, bar.suffix)
bar.next()
print(bar.index)
