# ryupy: pending
"""C1 方法包装候选：真实CPython描述符消费者、元数据修改与普通调用结果。"""
from collections import UserDict
from bridge_fixture.method_descriptor_contract import verify


class Record(UserDict):
    def sample(self, x, *, z=1):
        return self['offset'] + x + z

    @classmethod
    def kind(cls):
        return cls.__name__

    @staticmethod
    def twice(value):
        return value * 2

    @property
    def offset(self):
        return self['offset']


record = Record({'offset': 7})
assert verify(Record, record)
assert Record.kind() == 'Record'
assert record.twice(3) == 6
assert record.offset == 7
print('native method descriptors passed')
