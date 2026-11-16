# 由导入机制建立真实 __spec__；默认 files() 必须从此模块推断而非 __main__。
import importlib.resources as resources


def default_files():
    return resources.files()


def none_files():
    return resources.files(None)
