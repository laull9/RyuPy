# ryupy: pending
# M3.2 / AOT 生成器：局部变量生命周期、无用临时释放与安全控制流边界。

# 1. 跨 yield 活跃与不活跃局部变量状态
def liveness_scope(n):
    # 局部变量 a 在第一阶段存活
    a = 100
    temp_first = a * 2
    yield temp_first
    # temp_first 在此处已死，但 a 和新变量 b 存活
    b = 200
    yield a + b
    # 循环内局部变量
    i = 0
    while i < n:
        yield i
        i += 1
    return "done"

gen1 = liveness_scope(3)
results1 = []
try:
    while True:
        results1.append(next(gen1))
except StopIteration as stop:
    results1.append(stop.value)
print("liveness:", results1)

# 2. send 与局部变量更新
def send_accumulate(start):
    total = start
    while True:
        received = yield total
        if received is None:
            break
        total += received
    return total

gen2 = send_accumulate(10)
print("init:", next(gen2))
print("send 5:", gen2.send(5))
print("send 20:", gen2.send(20))
try:
    gen2.send(None)
except StopIteration as stop:
    print("accumulate final:", stop.value)

# 3. throw 与 finally 安全保护
def protected_generator():
    resources = ["res1", "res2"]
    try:
        yield resources[0]
        yield resources[1]
    except ValueError as e:
        yield f"caught:{e}"
    finally:
        resources.clear()
        print("finally executed")

gen3 = protected_generator()
print("prot 1:", next(gen3))
print("throw:", gen3.throw(ValueError("manual")))
print("prot next:", next(gen3, "exhausted"))

# 4. yield from 嵌套委托与局部变量
def sub_worker(limit):
    for idx in range(limit):
        yield idx * 10
    return "sub_done"

def delegator(limit):
    status = yield from sub_worker(limit)
    yield f"worker_result:{status}"

gen4 = delegator(3)
for item in gen4:
    print("delegated:", item)

# 5. 生成器表达式嵌套与立即求值边界
base = [1, 2, 3]
expr = (x * x for x in base)
base.append(4)
print("expr:", list(expr))

# 6. callback 闭包观察局部变量与 cell 共享
def callback_generator():
    observed = 500
    def inspector():
        return observed
    yield inspector()
    observed = 600
    yield inspector()

gen5 = callback_generator()
print("cb 1:", next(gen5))
print("cb 2:", next(gen5))

# 7. 未使用但 typed (bool/int) 静态临时运算
def typed_dead_temporaries(n):
    # 表达式中产生中间布尔/整型临时，但后续跨 yield 不再使用
    flag = (n > 0)
    unused_int = n + 42
    yield n
    # 恢复后继续正常计算，验证恢复端不会发生 typed 槽 null 拆箱故障
    yield n * 2

gen6 = typed_dead_temporaries(7)
print("typed 1:", next(gen6))
print("typed 2:", next(gen6))

# 8. send receiver 连续多挂起点接收与状态转移
def echo_receiver():
    val = yield "start"
    while val != "quit":
        val = yield f"received:{val}"
    return "done"

gen7 = echo_receiver()
print("echo 0:", next(gen7))
print("echo 1:", gen7.send("alpha"))
print("echo 2:", gen7.send("beta"))
try:
    gen7.send("quit")
except StopIteration as stop:
    print("echo final:", stop.value)
