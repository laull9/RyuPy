# BuiltinSum 流式迭代：逐项消费、无全量物化、严格交错与早停保证。

# 1. 基础数值序列与生成器累加
print("--- 1. basic numeric iterables ---")
print("sum list:", sum([1, 2, 3, 4]))
print("sum tuple:", sum((10, 20, 30)))
print("sum range:", sum(range(5)))
print("sum gen:", sum(x * 2 for x in range(5)))
print("sum floats:", sum([0.5, 1.25]))
print("sum bigint:", sum([2 ** 80, 2]))
print("sum bools:", sum([True, False, True]))

# 2. 非空 start 与空输入 start 对象身份保持
print("--- 2. start identity and values ---")
start_list = []
res_identity = sum([], start_list)
print("empty sum preserves start identity:", res_identity is start_list)
print("sum with int start:", sum([1, 2, 3], 100))
print("sum with kw start:", sum([1, 2, 3], start=100))
print("empty sum with default start:", sum([]))

# 3. 自定义元素 __radd__ 与生成器 yield 事件严格交错
print("--- 3. interleaved yield and __radd__ ---")
trace3 = []

class Item3:
    def __init__(self, value):
        self.value = value

    def __radd__(self, other):
        trace3.append(f"radd:{self.value}")
        return other + self.value

def gen3():
    for v in [10, 20, 30]:
        trace3.append(f"yield:{v}")
        yield Item3(v)

total3 = sum(gen3())
print("total3:", total3)
print("trace3:", trace3)

# 4. 第 2 次加法抛错后不再获取第 3 项 (早停)
print("--- 4. early stop on addition failure ---")
trace4 = []

class FailingItem4:
    def __init__(self, step, should_fail):
        self.step = step
        self.should_fail = should_fail

    def __radd__(self, other):
        trace4.append(f"add:{self.step}")
        if self.should_fail:
            raise ValueError(f"failed at step {self.step}")
        return other + self.step

def gen4():
    trace4.append("gen:1")
    yield FailingItem4(1, False)
    trace4.append("gen:2")
    yield FailingItem4(2, True)
    trace4.append("gen:3")
    yield FailingItem4(3, False)

try:
    sum(gen4())
except ValueError as e:
    trace4.append(f"caught:{e}")

print("trace4:", trace4)

# 5. 自定义 start 的 __add__ 与 __iadd__ 区分 (普通二元加法，不调用 __iadd__)
print("--- 5. __add__ vs __iadd__ on start ---")
class CustomStart:
    def __init__(self, val):
        self.val = val
        self.add_count = 0
        self.iadd_count = 0

    def __add__(self, other):
        self.add_count += 1
        return CustomStart(self.val + other)

    def __iadd__(self, other):
        self.iadd_count += 1
        self.val += other
        return self

start5 = CustomStart(10)
res5 = sum([1, 2, 3], start5)
print("res5 val:", res5.val)
print("start5 add_count:", start5.add_count)
print("start5 iadd_count:", start5.iadd_count)

# 6. 加法中修改被迭代 list，新元素可见且别名保持
print("--- 6. live list mutation during iteration ---")
lst6 = []

class Mutator6:
    def __init__(self, val):
        self.val = val

    def __radd__(self, other):
        if self.val == 1:
            lst6.append(Mutator6(3))
        return other + self.val

lst6.extend([Mutator6(1), Mutator6(2)])
total6 = sum(lst6)
print("total6:", total6)
print("lst6 len:", len(lst6))

# 7. iterator 中途异常与 __iter__ 单次调用
print("--- 7. single __iter__ and iterator exception ---")
trace7 = []

class SingleIterSource:
    def __init__(self):
        self.iter_calls = 0

    def __iter__(self):
        self.iter_calls += 1
        trace7.append("iter_called")
        def _generator():
            yield 1
            yield 2
            raise RuntimeError("stream broke")
            yield 3
        return _generator()

source7 = SingleIterSource()
try:
    sum(source7)
except RuntimeError as e:
    trace7.append(f"caught:{e}")

print("source7 iter_calls:", source7.iter_calls)
print("trace7:", trace7)
