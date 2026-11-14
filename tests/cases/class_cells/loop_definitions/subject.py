classes = []
methods = []
for index in range(3):
    class Repeated:
        def ident(self):
            return __class__
    classes.append(Repeated)
    methods.append(Repeated.ident)
