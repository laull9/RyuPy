# ryupy: pending
# 显式欠账：自定义真值对象的JSON选项调用顺序，当前仅开放bool/int/None。
import json
class Flag:
    def __init__(self, name):
        self.name = name
    def __bool__(self):
        print(self.name)
        return True
class Sink:
    def write(self, chunk):
        print("write", chunk)
json.dump([1], Sink(), skipkeys=Flag("skip"), ensure_ascii=Flag("ascii"),
          check_circular=Flag("circle"), allow_nan=Flag("nan"), sort_keys=Flag("sort"))
