import subject
first = subject.stream()
second = subject.stream()
assert next(first) == 10
subject.value = 20
assert next(second) == 20
assert first.send(3) == 23
subject.value = 30
assert second.send(4) == 34
first.close()
assert subject.events == [30]
subject.value = 40
second.close()
assert subject.events == [30, 40]
third = subject.nested()
assert next(third) == 40
subject.value = 50
assert third.send(5) == 55
third.close()
assert subject.events == [30, 40, 50]
print('generator owner survives suspension and delegation')
