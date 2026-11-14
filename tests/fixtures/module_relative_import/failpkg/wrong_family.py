class ModuleNotFoundError(RuntimeError):
    pass
error = ModuleNotFoundError('same display name different family')
error.name = __name__
raise error
