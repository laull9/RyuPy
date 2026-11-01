# 转换 TypeError 被替换时清理回调帧；其它异常保留原抛出位置。
import sys

class Number:
    def __int__(self):
        if sys.argv[1] == 'type':
            raise TypeError('原始转换错误')
        raise ValueError('原始数值错误')

print('%d' % Number())
