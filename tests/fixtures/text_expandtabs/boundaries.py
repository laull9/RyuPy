# ryupy: pending
# 独立旧债：保留CP完整正期待，不把以下行为算作六文件正常域；禁止改为期待NI。
import warnings


class Integer(int):
    def __index__(self):
        raise AssertionError('整数派生payload不调用覆盖index')


assert 'a\t'.expandtabs(Integer(4)) == 'a   '


class ReturnsInteger:
    def __index__(self):
        return Integer(4)


with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter('always')
    assert 'a\t'.expandtabs(ReturnsInteger()) == 'a   '
    assert len(captured) == 1
    assert captured[0].category is DeprecationWarning
    assert str(captured[0].message) == '__index__ returned non-int (type Integer).  The ability to return an instance of a strict subclass of int is deprecated, and may be removed in a future version of Python.'
# D009：CP保留两个独立代理码点，CLR相邻UTF16代理会合并；不修改期待。
joined = chr(0xD800) + chr(0xDC00)
expanded = (joined + '\t').expandtabs(4)
assert len(expanded) == 4
assert ord(expanded[0]) == 0xD800
assert ord(expanded[1]) == 0xDC00
assert expanded[2:] == '  '
try:
    'a\t'.expandtabs(tabsize=4, **{'tabsize': 8})
except TypeError as error:
    assert str(error) == "str.expandtabs() got multiple values for keyword argument 'tabsize'"
else:
    raise AssertionError('普通dict展开必须查重')
marker = LookupError('mapping-marker')


class BadMap:
    def keys(self):
        raise marker


try:
    str.expandtabs(1, **BadMap())
except LookupError as error:
    assert error is marker
else:
    raise AssertionError('mapping失败先于self校验')
print('expandtabs pending boundaries')
