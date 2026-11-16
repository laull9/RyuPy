# ryupy: pending
# 实际catch传播构建traceback链，链头/帧身份跨退出await稳定。
class Pause:
    def __await__(self):
        return (yield "cleanup")
class Manager:
    async def __aenter__(self):
        return self
    async def __aexit__(self, kind, value, tb):
        print(tb is value.__traceback__, tb is not None)
        self.saved = tb
        names = []
        while tb is not None:
            names.append(tb.tb_frame.f_code.co_name)
            print(tb.tb_lineno > 0, tb.tb_frame.f_code.co_filename.endswith("async_with_traceback.py"))
            tb = tb.tb_next
        print(names)
        await Pause()
        print(self.saved is value.__traceback__)
        return True
def failure():
    raise ValueError("failure")
async def operation():
    async with Manager():
        failure()
c = operation()
print(c.send(None))
try:
    c.send(None)
except StopIteration:
    pass
# 同步with的as解包失败同样进入exit，使用真实同一traceback头。
class Sync:
    def __enter__(self):
        return [1]
    def __exit__(self, kind, value, tb):
        print(kind.__name__, tb is value.__traceback__, tb is not None)
        return True
with Sync() as (left, right):
    print("unreachable")
print("sync-cleaned")
