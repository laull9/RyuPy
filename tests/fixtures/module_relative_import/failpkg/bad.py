import failpkg
failpkg.events.append('bad')
failpkg.attempts += 1
if failpkg.attempts == 1:
    raise RuntimeError('bad init')
