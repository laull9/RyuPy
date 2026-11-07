class Missing(ModuleNotFoundError):
    pass
error = Missing('own missing name')
error.name = __name__
raise error
