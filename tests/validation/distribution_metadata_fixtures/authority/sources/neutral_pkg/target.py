# 冷候选必须等到load才初始化，重复load保同一对象。
print('target-init')
def value():
    return 17
