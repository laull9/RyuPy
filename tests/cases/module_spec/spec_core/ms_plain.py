# 普通源模块初始化时元数据已经可见。
ready = (__spec__.name == __name__ and __loader__ is __spec__.loader and __package__ == __spec__.parent)
