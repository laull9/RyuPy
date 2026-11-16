def generate(kind):
    try:
        try:
            value = yield 'ready'
            print(value)
            yield 'sent'
        except kind:
            yield 'thrown'
    finally:
        print('closed')
it = generate(ValueError)
print(next(it))
print(it.send('payload'))
print(it.throw(ValueError('injected')))
it.close()
