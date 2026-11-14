class Base:
    def read(self):
        return 'base'
class Child(Base):
    def read(self):
        return super().read(), __class__
