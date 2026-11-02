# 根据旧聊天摘要构造的候选程序，非用户原文件。
low = 1
high = 100
count = 0
while low <= high:
    guess = (low + high) // 2
    count += 1
    print(f'guess {guess}')
    answer = input('answer y/d/x: ')
    if answer == 'y':
        break
    elif answer == 'd':
        high = guess - 1
    elif answer == 'x':
        low = guess + 1
    else:
        print('invalid')
        count -= 1
else:
    print('error')
print(f'count {count}')
