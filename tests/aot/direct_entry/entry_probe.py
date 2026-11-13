"""同一原生入口验证参数、退出、异常和退出回调后的标准输出刷新。"""
import atexit
import sys


def finish():
    print('|callback', end='')


def fail():
    raise ValueError('direct-entry-error')


atexit.register(finish)
mode = sys.argv[1]
if mode == 'argv':
    for arg in sys.argv[2:]:
        print(len(arg), arg)
elif mode == 'exit7':
    sys.exit(7)
elif mode == 'exittext':
    sys.exit('direct-entry-text')
elif mode == 'error':
    fail()
elif mode == 'flush':
    print('buffered', end='')
else:
    print('entry-ok')
