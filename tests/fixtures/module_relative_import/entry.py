import pkg.subject
import pkg.plain
print(pkg.events)
print(pkg.subject.read())
pkg.plain.value['x'] = 9
print(pkg.subject.read())
pkg.plain.value = {'x': 11}
print(pkg.subject.read())
