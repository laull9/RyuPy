# 纯普通函数模块：不得由class/del提前遮蔽Instance生成器handler守卫。
def generate(kind):
    try:
        yield "ready"
        raise ValueError("caught")
    except kind:
        yield "handled"
