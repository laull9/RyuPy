# 列表推导式私有 collector ListAppend 直调优化端到端验证。
# 覆盖：基础/嵌套/多for与if；外部内部sideeffect顺序；lambda晚绑定与外部capture；
# 元素抛错与短路边界；自定义类append与普通变量result保持；module-global rebinding。

events = []


def f_iter():
    events.append("iter_call")
    return [1, 2, 3, 4]


def f_if(x):
    events.append(f"if_{x}")
    return x % 2 == 1


def f_elt(x):
    events.append(f"elt_{x}")
    return x * 10


# 1. 基础推导式与副作用求值顺序
res1 = [f_elt(x) for x in f_iter() if f_if(x)]
print(res1)
print(events)

# 2. 嵌套推导式与多重 for / if 过滤
res2 = [(i, j) for i in range(4) for j in range(i) if (i + j) % 2 == 0]
print(res2)

# 3. lambda 晚绑定：推导式作用域内的变量捕获与外部 capture
funcs = [lambda: item for item in range(4)]
print([f() for f in funcs])

g_multiplier = 10


def test_captures():
    outer_factor = 2

    def inner():
        nonlocal outer_factor
        r = [x * outer_factor * g_multiplier for x in range(3)]
        outer_factor += 1
        return r

    return inner


closure_fn = test_captures()
print(closure_fn())
print(closure_fn())

# 4. 元素求值抛错：在此之前的副作用已生效，在此之后的元素不再求值
err_events = []


def fault_elt(x):
    err_events.append(f"eval_{x}")
    if x == 2:
        raise ValueError("stop_here")
    return x


try:
    _ = [fault_elt(x) for x in range(5)]
except ValueError as exc:
    err_events.append(f"caught_{exc}")

print(err_events)

# 5. 自定义类的 append 与用户变量 result 保持通用分派语义
custom_events = []


class CustomCollector:
    def __init__(self):
        self.data = []

    def append(self, val):
        custom_events.append(f"custom_{val}")
        self.data.append(val)


c = CustomCollector()
c.append("first")
c.append("second")
print(custom_events, c.data)

result = [10, 20]
result.append(30)
print(result)

# 6. 模块全局变量 rebinding 不影响推导式内部私有 collector
list = "initial binding"
original_list = list
list = "rebound"
comp_under_rebinding = [x * 3 for x in range(3)]
list = original_list
print(comp_under_rebinding)

# 7. 默认参数在每次元素求值时绑定，实例方法中的self仍按闭包协议捕获。
default_funcs = [lambda value=x: value for x in range(4)]
print([f() for f in default_funcs])


class Owner:
    def __init__(self):
        self.scale = 3

    def collect(self):
        return [self.scale * x for x in range(4)]


print(Owner().collect())
