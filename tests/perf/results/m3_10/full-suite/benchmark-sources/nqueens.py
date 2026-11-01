# bench: category=algorithms
# bench: desc=8 皇后回溯求全部解（递归 + 列表标记），重复 100 次
def solve(n, row, cols, diag1, diag2):
    if row == n:
        return 1
    count = 0
    for c in range(n):
        d1 = row + c
        d2 = row - c + n - 1
        if not cols[c] and not diag1[d1] and not diag2[d2]:
            cols[c] = True
            diag1[d1] = True
            diag2[d2] = True
            count += solve(n, row + 1, cols, diag1, diag2)
            cols[c] = False
            diag1[d1] = False
            diag2[d2] = False
    return count


def queens(n):
    cols = []
    diag1 = []
    diag2 = []
    for _ in range(n):
        cols.append(False)
    for _ in range(2 * n - 1):
        diag1.append(False)
        diag2.append(False)
    return solve(n, 0, cols, diag1, diag2)


total = 0
for _ in range(100):
    total += queens(8)
print(total)
