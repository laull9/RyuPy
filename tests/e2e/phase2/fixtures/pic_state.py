# PIC 测试用的模块：入口会在运行期替换 base，验证模块属性不做缓存。
def base():
    return "base"


counter = 0
