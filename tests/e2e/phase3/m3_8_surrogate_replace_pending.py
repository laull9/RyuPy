# ryupy: pending
# 独立方法传播缺口：replace新建结果还未迁移精确码点边界，原值保持正确。
pair = '\ud83d\ude00'
result = (pair + 'x').replace('x', 'y')
print(len(pair), [ord(item) for item in pair])
print(len(result), [ord(item) for item in result], result == pair + 'y')
