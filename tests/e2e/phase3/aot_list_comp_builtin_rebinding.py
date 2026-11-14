# ryupy: pending
# 模块内重绑定list时，先前未绑定读取应回退到内建list对象；保留JIT发现的原始反例。
original_list = list
list = "rebound"
comp_under_rebinding = [x * 3 for x in range(3)]
list = original_list
print(comp_under_rebinding)
