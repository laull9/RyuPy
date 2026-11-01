# ryupy: pending
# 后续缓存hash复制竖切：源集合已缓存的hash不能重新执行用户代码。
log = []


class Key:
    def __hash__(self):
        log.append("hash")
        return 1


key = Key()
source = {key}
log.clear()
result = {*source}
print(len(result), log)


def fail(self):
    raise ValueError("不应重新hash")


Key.__hash__ = fail
result = {0, *source}
print(len(result), log)
