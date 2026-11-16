# await 只使用类型槽，验证返回的迭代器并保留终值；各种错误不会同步执行函数体。
async def immediate(value):
    return value
async def use(value):
    return await value
def finish(c):
    try:
        c.send(None)
    except StopIteration as e:
        print("result", e.value)
    except (TypeError, RuntimeError) as e:
        print(type(e).__name__, str(e))
class Iterator:
    def __iter__(self):
        raise AssertionError("await cannot call iter")
    def __next__(self):
        raise StopIteration(17)
class Awaitable:
    def __await__(self):
        print("await-slot")
        return Iterator()
a = Awaitable()
a.__await__ = None
finish(use(a))
finish(use([]))
class Bad:
    def __await__(self):
        return []
finish(use(Bad()))
raw = immediate(9)
class BadCoroutine:
    def __await__(self):
        return raw
finish(use(BadCoroutine()))
raw.close()
raw = immediate(23)
finish(use(raw))
finish(use(raw))
raw = immediate(1)
raw.close()
finish(use(raw))
class Pause:
    def __await__(self):
        value = yield "waiting"
        return value
async def suspended():
    return await Pause()
raw = suspended()
first = use(raw)
print(first.send(None))
finish(use(raw))
try:
    first.send(31)
except StopIteration as e:
    print("first", e.value)
finish(use(raw))
async def reentrant():
    try:
        running.send(None)
    except ValueError as e:
        print(str(e))
running = reentrant()
finish(running)
async def leaked():
    raise StopIteration(12)
finish(leaked())
class BadGenerator:
    def __await__(self):
        if False:
            yield None
        raise StopIteration(13)
finish(use(BadGenerator()))
async def named_error():
    raise RuntimeError("generator raised StopIteration")
finish(named_error())

# 显式 __await__ 迭代器也必须保留完成值，不能让 next 的普通游标适配吞掉它。
raw = immediate(44)
wrapper = raw.__await__()
print(iter(wrapper) is wrapper)
try:
    next(wrapper)
except StopIteration as e:
    print("wrapper-result", e.value)
raw = suspended()
wrapper = raw.__await__()
print(next(wrapper))
try:
    wrapper.send(45)
except StopIteration as e:
    print("wrapper-send", e.value)

async def typed(value: int) -> int:
    return value + 1
finish(typed(45))
# 外部关闭子协程后，同一对象进入完成态；其它 await 报复用错误。
raw = suspended()
first = use(raw)
print(first.send(None))
raw.close()
finish(use(raw))
first.close()
# 协程自身忽略 GeneratorExit 时使用协程名称，随后显式关闭其第二暂停点。
async def ignores_exit():
    try:
        await Pause()
    except GeneratorExit:
        await Pause()
raw = ignores_exit()
print(raw.send(None))
try:
    raw.close()
except RuntimeError as e:
    print(str(e))
raw.close()

async def close_reentrant():
    try:
        await Pause()
    finally:
        try:
            closing.send(None)
        except ValueError as e:
            print(str(e))
closing = close_reentrant()
print(closing.send(None))
closing.close()
