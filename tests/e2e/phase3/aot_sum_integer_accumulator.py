# ryupy: pending
# AOT 原生整数累加器优化 e2e 验证用例 (CPython 3.12.13 即时比对)
# 包含连续整数累加、小整数规范、正负抵消、溢出与回落、混类型及协议早停。

# 1. 连续原生整数与大整数求和
print("--- 1. continuous integers ---")
print("sum small positive:", sum([1, 2, 3, 4, 5]))
print("sum large streak:", sum(range(100, 110)))
print("sum offset streak:", sum([1000, 2000, 3000]))

# 2. 负数抵消与小整数单例范围
print("--- 2. cancellation and smallint ---")
zero_ref = 0
min_ref = -5
max_ref = 256
cancelled = sum([500, -500])
print("cancelled sum:", cancelled)
print("cancelled is zero:", cancelled is zero_ref)
min_small = sum([-2, -3])
print("min smallint (-5):", min_small, min_small is min_ref)
max_small = sum([250, 6])
print("max smallint (256):", max_small, max_small is max_ref)

# 3. 超出 64 位整数范围的溢出与回退
print("--- 3. overflow to big int and fallback ---")
i64_max = (1 << 63) - 1
i64_min = -(1 << 63)
overflow_pos = sum([i64_max, 10])
print("positive overflow:", overflow_pos > i64_max)
overflow_neg = sum([i64_min, -10])
print("negative overflow:", overflow_neg < i64_min)
fallback = sum([i64_max, 100, -100, -1])
print("fallback to i64:", fallback == i64_max - 1)

# 4. 混 float 与 bool
print("--- 4. mixed float and bool ---")
print("mix bool:", sum([10, True, False, 20]))
print("mix float:", sum([10, 2.5, 20]))

# 5. 长整数 streak 后自定义 item 能看到准确累加值
print("--- 5. long streak followed by custom item ---")
observed_totals = []

class CustomObserver:
    def __init__(self, tag):
        self.tag = tag

    def __radd__(self, other):
        observed_totals.append((self.tag, other))
        return other + 7

res_obs = sum([10, 20, 30, CustomObserver("item1")])
print("observed result:", res_obs)
print("observed totals:", observed_totals)

# 6. 自定义 start 使用 __add__ 从不使用 __iadd__
print("--- 6. custom start uses __add__ not __iadd__ ---")
class CustomStartAcc:
    def __init__(self, val):
        self.val = val
        self.add_count = 0
        self.iadd_count = 0

    def __add__(self, other):
        self.add_count += 1
        return CustomStartAcc(self.val + other)

    def __iadd__(self, other):
        self.iadd_count += 1
        self.val += other
        return self

start_obj = CustomStartAcc(100)
res_start = sum([1, 2, 3], start_obj)
print("res_start val:", res_start.val)
print("start_obj add_count:", start_obj.add_count)
print("start_obj iadd_count:", start_obj.iadd_count)

# 7. 空迭代器与 start 引用一致性
print("--- 7. empty iterable preserves start identity ---")
shared_start = [1, 2, 3]
print("empty preserves list start:", sum([], shared_start) is shared_start)
print("empty default start:", sum([]))

# 8. 严格交错与加法失败早停
print("--- 8. early stop on add failure ---")
trace8 = []

class FailingItem8:
    def __init__(self, val, fail):
        self.val = val
        self.fail = fail

    def __radd__(self, other):
        trace8.append(f"add:{self.val}")
        if self.fail:
            raise ValueError(f"failed at {self.val}")
        return other + self.val

def gen8():
    trace8.append("yield:1")
    yield 10
    trace8.append("yield:2")
    yield 20
    trace8.append("yield:3")
    yield FailingItem8(30, True)
    trace8.append("yield:4")
    yield 40

try:
    sum(gen8())
except ValueError as e:
    trace8.append(f"caught:{e}")

print("trace8:", trace8)

# 原生累计后用户协议返回已有大整数对象，结束时仍返回同一对象。
shared_integer = int("888888")
class SharedIntegerResult:
    def __radd__(self, other):
        return shared_integer
print("shared callback result:", sum([10, 20, SharedIntegerResult()]) is shared_integer)
print("negative overflow recovery:", sum([i64_min, -5, 5, 10]) == i64_min + 10)
zero = 0
print("empty default canonical:", sum([]) is zero)
