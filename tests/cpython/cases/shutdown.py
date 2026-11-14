"""CP1b：解释器退出时 Python 持有的编译回调仍保持可用。"""
import atexit


def compiled_exit():
    print('CP1b atexit 编译回调通过')


atexit.register(compiled_exit)
print('CP1b 退出前')
