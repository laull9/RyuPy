# M2.1：定义期默认值、完整参数绑定和错误实参。
def collect(first, /, second=2, *items, scale=1, **options):
    return (first + second + sum(items)) * scale, options


def remember(value, history=[]):
    history.append(value)
    return history[:]


print(collect(1, 3, 4, 5, scale=2, label="ok"))
print(collect(*[1, 2], **{"scale": 3}))
print(remember(1), remember(2))
try:
    collect(1, 2, second=3)
except TypeError:
    print("duplicate argument")
try:
    collect(first=1)
except TypeError:
    print("positional only")
