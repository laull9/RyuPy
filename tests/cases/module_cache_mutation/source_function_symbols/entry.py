# ryupy: pending
# 普通源码静态直调也须尊重替换；旧导入函数保原环境。
import sys
import mc_subject
from mc_subject import read as old_read
original = mc_subject
import mc_donor
sys.modules['mc_subject'] = mc_donor
import mc_subject as current
from mc_subject import read as new_read
assert current is mc_donor and new_read is mc_donor.read
assert new_read() == 29 and old_read() == 13
def local_read():
    from mc_subject import read
    return read()
assert local_read() == 29
original.value = 31
mc_donor.value = 47
assert old_read() == 31 and new_read() == 47 and local_read() == 47
assert sys.modules['mc_subject'] is mc_donor
print('source function rebinding preserves old and new owners', True)
