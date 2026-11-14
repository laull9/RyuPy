# 当前线程身份只比较稳定性，不比较各进程的平台相关数值。
from _thread import get_ident
import sys

identity = get_ident()
print(isinstance(identity, int), identity != 0, get_ident() == identity)
v = sys.version_info
print(sys.hexversion == (v.major << 24 | v.minor << 16 | v.micro << 8 | 0xF0))
print(sys.hexversion >= 0x03000000)
for mode in range(2):
    try:
        if mode == 0:
            get_ident(1)
        else:
            get_ident(value=1)
    except TypeError:
        print('argument error')
