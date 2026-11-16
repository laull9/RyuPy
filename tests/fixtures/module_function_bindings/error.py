# 发布失败保留旧alias；调用旧函数的未捕获错误保留完整context与cause链。
selected = 'seed'
def selected():
    raise LookupError('old callable')
saved = selected
def failed_default():
    raise ValueError('default rejected')
try:
    def selected(value=failed_default()):
        return value
except ValueError:
    print('preserved', selected is saved)
    try:
        saved()
    except LookupError as cause:
        raise RuntimeError('outer failure') from cause
