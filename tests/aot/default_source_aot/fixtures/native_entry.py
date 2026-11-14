# 单ELF双模式；不使用exec或测试host。
import sys
from contract import semantic, boundary
if sys.argv[1] == "semantic":
    semantic()
elif sys.argv[1] == "boundary":
    boundary()
else:
    raise ValueError("unknown contract mode")
