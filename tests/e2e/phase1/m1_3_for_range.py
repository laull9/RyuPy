# ryupy: pending
# M1.3：for range 降级为原生计数循环。注意负步长与空区间。
total = 0
for i in range(10):
    total += i
print(total)

for i in range(2, 5):
    print(i)

for i in range(10, 0, -3):
    print(i)

for i in range(5, 5):
    print("never")

acc = 0
for i in range(100):
    if i % 2 == 0:
        continue
    if i > 20:
        break
    acc += i
print(acc)
