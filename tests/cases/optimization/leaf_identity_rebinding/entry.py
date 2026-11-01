import left
import right

old = left.twice
print(left.invoke('1234567'), old is left.alias)
left.twice = right.twice
print(left.invoke('1234567'))
left.alias = left.twice
print(left.invoke('1234567'))
left.alias = old
print(left.invoke('1234567'))

def rebind():
    print('rebind')
    left.twice = right.twice
    left.alias = right.twice
    return '1234567'

print(left.invoke_callback(rebind))
print(left.invoke('1234567'))
left.alias = old
print(left.invoke('1234567'), left.alias is old)
