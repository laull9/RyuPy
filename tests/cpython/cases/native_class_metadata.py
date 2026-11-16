"""C1 元类和 prepared mapping 的真实类元数据独立于函数定义模块。"""
from bridge_fixture.class_construction import TransformBase


class MetadataClass(TransformBase):
    def method(self):
        return 42
    function_module = method.__module__


assert MetadataClass.__module__ == 'mapped.' + __name__
assert MetadataClass.__qualname__ == 'rebuilt.MetadataClass'
assert MetadataClass.__name__ == 'renamed_MetadataClass'
assert MetadataClass.function_module == __name__
assert MetadataClass().method() == 42
print('native class metadata passed')
