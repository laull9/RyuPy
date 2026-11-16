class Missing(ModuleNotFoundError):
    name = __name__
raise Missing('class missing name')
