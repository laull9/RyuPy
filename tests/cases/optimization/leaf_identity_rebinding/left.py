def twice(value):
    return value + value

alias = twice

def invoke(value):
    return alias(len(value))

def invoke_callback(callback):
    return alias(len(callback()))
