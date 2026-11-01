# ryupy: pending
# 超过17项的动态键分块需要缓存hash合并，不能把尾块逐项插入冒称兼容。
log = []


class Key:
    def __init__(self, value):
        self.value = value

    def __hash__(self):
        log.append(self.value)
        return self.value


def work():
    return {Key(0): 0, 1: 1, 2: 2, 3: 3, 4: 4, 5: 5, 6: 6, 7: 7,
            8: 8, 9: 9, 10: 10, 11: 11, 12: 12, 13: 13, 14: 14,
            15: 15, 16: 16, Key(17): 17, 18: (yield "tail")}


it = work()
print(next(it), log)
try:
    it.send(18)
except StopIteration as error:
    print(len(error.value), log)
