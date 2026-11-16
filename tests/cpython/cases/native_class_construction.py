"""C1 两阶段原生类构造：prepare 在类体前、mapping 单读、元类普通属性与重入。"""
from bridge_fixture.class_construction import Base, body, reset, snapshot

reset()
class Constructed(Base):
    body_value = body()
    copied = prepared + body_value
    __metaclass__ = 17

    @classmethod
    def on_construct(cls):
        return cls

assert Constructed.copied == 18
assert Constructed.__metaclass__ == 17
assert snapshot() == (['prepare:Constructed', 'body', 'write:body_value', 'new:Constructed', 'init_subclass:Constructed'], ['prepared', 'body_value'])
print('native class construction passed')
