from importlib.resources import files
import failure_events

print('before', failure_events.events)
try:
    files('failpkg')
except ValueError as error:
    print('failed', str(error))
print('after-failure', failure_events.events)
print('retry', files('failpkg').joinpath('data.txt').read_text(encoding='utf-8').strip())
print('cached', files('failpkg').joinpath('data.txt').read_text(encoding='utf-8').strip())
print('events', failure_events.events)
assert failure_events.events == [1, 2]
