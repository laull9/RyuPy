public = 'seed'
def public(value=1):
    return value + 1
alias = public
def read():
    return public
def mutate(value):
    global public
    public = value
def clean(value):
    return ['clean', value]
def call_clean(value):
    return clean(value)
