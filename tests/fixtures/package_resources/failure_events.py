events = []
attempts = 0

def begin():
    global attempts
    attempts += 1
    events.append(attempts)
    if attempts == 1:
        raise ValueError('init-failed')
