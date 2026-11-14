# Ryu专用错误现场：CP支持此语法，不把NI当作CP差分成功。
import re


def leaf():
    re.compile('(?i:a)')


def outer():
    leaf()


try:
    raise ValueError('outer context')
except ValueError:
    outer()
