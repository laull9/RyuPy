# 普通函数IMPORT_NAME传D/None，测试真实caller上下文而非entry D。
def fetch():
    from relative_leaf import first
    return first
