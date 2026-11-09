# 无class的Instance模块；保留原issue与global删除helper。
import warnings

events = []

def issue(message, category):
    warnings.warn(message, category)

def remove_name():
    global __name__
    del __name__
