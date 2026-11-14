# ryupy: pending
# 真正恢复生成器，确保不是只验创建。
import subject
obj = subject.Child()
assert next(obj.values()) is obj
print('pending generator alias frame')
