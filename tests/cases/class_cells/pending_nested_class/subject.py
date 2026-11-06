def factory():
    class Local:
        def read(self):
            return __class__
    return Local
first = factory()
second = factory()
