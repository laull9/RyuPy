# 既有Legacy同名参数回调；不得按globals名称授新能力。
def invoke(globals):
    return globals()

def invoke_one(globals):
    return globals(1)
