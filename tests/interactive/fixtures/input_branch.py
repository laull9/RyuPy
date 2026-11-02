# 独立隔离输入与比较/分支，不是用户程序。
for index in range(7):
    answer = input('answer: ')
    print(repr(answer), answer == 'y', answer == 'd', answer == 'x')
    if answer == 'y':
        print('branch-y')
        break
    elif answer == 'd':
        print('branch-d')
    elif answer == 'x':
        print('branch-x')
    else:
        print('branch-invalid')
