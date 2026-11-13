"""默认代理容器比较的显式边界，不让未知键或set混合比较递归到本机栈溢出。"""
import ffi_demo
import set_limits

try:
    ffi_demo.custom_key_dict() == {1: 2}
except NotImplementedError as error:
    assert 'exact immutable keys' in str(error)
else:
    raise AssertionError('自定义键域未明确拒绝')
print('活代理比较边界诊断通过')
