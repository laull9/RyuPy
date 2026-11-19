import subject
import helpers
assert subject.Subject is helpers.Replacement
original = helpers.escaped[-1]
assert helpers.escaped[0]() is original
assert helpers.escaped[1](None) is original
assert helpers.phase == ['module value', 'module value', 'expression empty', 'method empty', 'class filled']
print('class definition phases and lambda scope')
