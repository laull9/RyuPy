# bench: category=numeric
# bench: desc=埃氏筛 200 万以内素数，布尔列表下标读写 + while 循环
def sieve(limit):
    flags = [True] * (limit + 1)
    flags[0] = False
    flags[1] = False
    i = 2
    while i * i <= limit:
        if flags[i]:
            j = i * i
            while j <= limit:
                flags[j] = False
                j += i
        i += 1
    count = 0
    for f in flags:
        if f:
            count += 1
    return count


print(sieve(2000000))
