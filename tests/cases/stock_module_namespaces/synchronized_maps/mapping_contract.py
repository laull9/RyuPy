"""使用两种键和值布局穿过真实 CPython 写入，不依赖测试模块专用桥。"""
import json
import operator
from types import SimpleNamespace


def ordered_mutations():
    """较大的字典覆盖不变快路，以及双方的覆盖、删除和重插。"""
    table = {index: index + 800 for index in range(1100)}
    holder = SimpleNamespace(table=table)
    assert holder.table is table
    for _ in range(8):
        assert len(holder.table) == 1100
        assert table[18] == 818
    table[1] = "托管覆盖"
    assert operator.getitem(holder.table, 1) == "托管覆盖"
    del table[2]
    table[2] = "托管重插"
    assert list(json.loads(json.dumps(holder.table)))[-1] == "2"
    operator.delitem(holder.table, 5)
    operator.setitem(holder.table, 5, "外部重插")
    assert list(table)[-2:] == [2, 5]
    assert table[5] == "外部重插"


def shared_values():
    """两个键保同一子容器；异常保持已提交的外部改动和嵌套身份。"""
    key = ("固定", 23)
    child = []
    table = {key: child, "保留": child}
    holder = SimpleNamespace(payload=table)
    assert holder.payload is table and operator.getitem(holder.payload, key) is child
    operator.setitem(holder.payload, "外部新增", child)
    try:
        operator.delitem(holder.payload, "缺失")
    except KeyError as error:
        assert error.args[0] == "缺失"
    else:
        raise AssertionError("缺键删除必须失败")
    child.append("同一对象")
    assert table["外部新增"] is table["保留"]
    assert operator.getitem(holder.payload, "外部新增")[0] == "同一对象"
    assert list(table)[0] is key
