"""基础 object 槽搬迁合同：真实 UserDict 布局、super、属性身份及错误。"""
from collections import UserDict


class Record(UserDict):
    def __new__(cls):
        return super().__new__(cls)

    def __init__(self):
        super().__init__()

    def __setattr__(self, name, value):
        return super().__setattr__(name, value)

    def __delattr__(self, name):
        return super().__delattr__(name)


record = Record()
record.payload = record
print(record.payload is record, isinstance(record, UserDict), type(record) is Record)
record['answer'] = 42
print(record['answer'])
del record.payload
try:
    record.payload
except AttributeError as error:
    print(type(error).__name__)
try:
    del record.payload
except AttributeError as error:
    print(type(error).__name__)
print('object slots complete')
