value = 10
events = []

def stream():
    try:
        sent = yield value
        yield value + sent
    finally:
        events.append(value)

def nested():
    yield from stream()
