# 与标准库清理临时 import 名字同型，不能把删除当作丢弃静态元数据。
import provider as imported
from provider import work as imported_work
saved_module = imported
saved_function = imported_work
del imported, imported_work

def read_module():
    return imported

def read_function():
    return imported_work

def restore():
    global imported, imported_work
    import provider as imported
    from provider import work as imported_work
