# 由外部loader设置真实builtins，函数保本次定义捕获的环境。
def run(error):
    try:
        raise error
    except Target:
        return 'caught'
    except BaseException:
        return 'other'
