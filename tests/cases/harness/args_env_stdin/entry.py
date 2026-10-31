# 框架自检：参数、环境变量与标准输入的端到端传递。
import os
import sys

print(sys.argv[1:])
print(os.environ["RYUPY_CASE_ENV"])
print(input())
