# 已知差异独立观测：CPython可能重新装箱空sum的整数start，RyuPy原路径保留身份。
# 不属于普通CP差分通过门；候选必须与同提交关闭feature臂一致，不能隐藏或新增差异。
start = 12345
print(sum((x * 2 for x in range(0)), start) is start)
