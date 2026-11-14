# 同名递归每次调用仍是不同activation。
def recur(n):
    if n == 0:
        raise ValueError("recursive")
    recur(n - 1)

try:
    recur(2)
except ValueError as caught:
    result = caught
