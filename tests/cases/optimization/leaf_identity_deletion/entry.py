# ryupy: pending
import left
import right

old = left.twice
print(left.invoke(7), old is left.alias)
left.twice = right.twice
print(left.invoke(7))
left.alias = left.twice
print(left.invoke(7))
left.alias = old
print(left.invoke(7))

def rebind():
    print('rebind')
    left.twice = right.twice
    left.alias = right.twice
    return 7

print(left.invoke_callback(rebind))
print(left.invoke(7))
left.alias = old
del left.twice
print(left.invoke(7), left.alias is old)
try:
    left.twice
except AttributeError:
    print('deleted-public-name')
