# ryupy: pending
# 自定义hash/eq键的完整CP差分保留：新**合并须复用已存hash并在碰撞中保持身份和回调次数。
events = []
class Key:
    def __init__(self, label):
        self.label = label
    def __hash__(self):
        events.append('hash:' + self.label)
        return 37
    def __eq__(self, other):
        events.append('eq:' + self.label + ':' + other.label)
        return self.label == other.label
first = Key('first')
second = Key('second')
source = {first: 1}
other = {second: 2}
events.clear()
result = {**source, **other}
print([(key.label, value) for key, value in result.items()])
print(events)
events.clear()
result = {'prefix': 0, **source, first: 3}
print([(key.label if isinstance(key, Key) else key, value) for key, value in result.items()])
print(events)
class Mapping:
    def keys(self):
        return [first]
    def __getitem__(self, key):
        return 4
events.clear()
result = {**Mapping()}
print([(key.label, value) for key, value in result.items()])
print(events)
