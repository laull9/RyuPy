# ryupy: pending
# 类体条件执行必须使用实际命名空间，不能预先把两个分支都执行。
log = []
x = '模块'
def condition():
    log.append('condition')
    return False
class Choice:
    before = x
    if condition():
        x = '不可达'
    elif True:
        selected = '分支'
    else:
        selected = '错误'
    after = x
    if True:
        x = '类'
    final = x
print(Choice.before, Choice.selected, Choice.after, Choice.final, log)
