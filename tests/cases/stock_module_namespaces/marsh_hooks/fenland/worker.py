# 相对层级与raw name由IMPORT_NAME原样传hook。
def relative():
    from .channel import finish
    return finish
