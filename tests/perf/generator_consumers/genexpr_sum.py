# bench: category=phase3
# bench: desc=原始generators第三臂：百万项生成器表达式sum
# 手写源与原始工作负载由test_generator_consumers.py的AST合同绑定。
total = 0
total += sum(x * 2 for x in range(1000000))
print(total)
