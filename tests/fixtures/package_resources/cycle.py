from importlib.resources import files
import cycle_events

print('before', cycle_events.events)
print('first', files('cyclepkg').joinpath('data.txt').read_text(encoding='utf-8').strip())
print('second', files('cyclepkg').joinpath('data.txt').read_text(encoding='utf-8').strip())
print('events', cycle_events.events)
assert cycle_events.events == ['begin', 'cycle-data', 'end']
