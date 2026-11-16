# await 实参前绑定原方法并快照混合 *args，恢复后不重读已变更对象。
class Pause:
    def __await__(self):
        return (yield "waiting")
class Target:
    def method(self, value):
        return ["original", value]
def replacement(self, value):
    return ["replacement", value]
target = Target()
async def method_call(receiver):
    return receiver.method(await Pause())
coroutine = method_call(target)
print(coroutine.send(None))
Target.method = replacement
try:
    coroutine.send(7)
except StopIteration as error:
    print(error.value)
values = [1]
def collect(*items):
    return list(items)
async def mutate():
    values.append(2)
    result = await Pause()
    values.append(3)
    return result
async def star_call():
    return collect(*values, await mutate())
coroutine = star_call()
print(coroutine.send(None))
print(values)
try:
    coroutine.send(4)
except StopIteration as error:
    print(error.value)
print(values)

# 唯一 * 源后只有关键字时，CPython 将迭代展开留到关键字 await 恢复后。
# 保留这个求值时序：先求源表达式、mutate、暂停、恢复，再运行源的 __iter__。
def keyword_collect(*items, k):
    events.append("collect")
    return items, k
async def mutate_keyword():
    events.append("mutate")
    values.append(2)
    result = await Pause()
    events.append("resume")
    return result
async def unique_list_keyword():
    return keyword_collect(*values, k=await mutate_keyword())
class ObservedStar:
    def __iter__(self):
        events.append("iter")
        return iter(values)
def observed_source():
    events.append("source")
    return ObservedStar()
async def unique_observed_keyword():
    return keyword_collect(*observed_source(), k=await mutate_keyword())
def finish_keyword(coroutine):
    print(coroutine.send(None))
    print(values, events)
    try:
        coroutine.send(3)
    except StopIteration as error:
        print(error.value)
    print(events)
values = [1]
events = []
finish_keyword(unique_list_keyword())
values = [1]
events = []
finish_keyword(unique_observed_keyword())
