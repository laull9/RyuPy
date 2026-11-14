class Missing(ModuleNotFoundError):
    name = 'unrelated'
error = Missing('instance overrides non-data class field')
error.name = __name__
raise error
