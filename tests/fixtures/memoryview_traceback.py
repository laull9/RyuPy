# memoryview重写转换错误，但不能泄漏已吞__index__的帧。
import sys
class IndexFailure:
    def __index__(self):
        if sys.argv[1] == 'type':
            raise TypeError('原始类型错误')
        elif sys.argv[1] == 'value':
            raise ValueError('原始数值错误')
        elif sys.argv[1] == 'overflow':
            raise OverflowError('原始溢出错误')
        else:
            raise RuntimeError('原样运行错误')
view = memoryview(bytearray(b'a'))
view[0] = IndexFailure()
