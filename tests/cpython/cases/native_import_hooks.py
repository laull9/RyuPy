# ryupy: pending
"""C1 原生导入钩子：真实包、循环、失败重试、CP sys.modules 权威与关闭时首次反向导入。"""
from collections import UserDict
from bridge_fixture.native_import_contract import verify

# 冻结闭包内的潜在导入；第一次真实导入由 CPython 反向调用触发。
if False:
    import native_import_package.child
    import native_import_package.cycle
    import native_import_package.flaky
    import native_import_package.shutdown
    import native_import_empty
    import native_import_flat


class Record(UserDict):
    def ping(self):
        return 'wrapper alive'


assert Record().ping() == 'wrapper alive'
assert verify()
print('native import hooks passed')
