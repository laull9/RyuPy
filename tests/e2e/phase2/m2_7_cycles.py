# ryupy: pending
# M2.7：循环中看见部分初始化对象，重复导入不重复执行。
from fixtures.cycle_case import left, right
import fixtures.cycle_case.left as again

print(left.phase, right.seen, left is again)
