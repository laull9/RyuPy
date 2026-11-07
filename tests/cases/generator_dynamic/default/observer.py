# 普通非生成器前置：只观察活异常身份/上下文，不比较traceback。
class E(Exception):
    pass


class F(Exception):
    pass


def observe_active():
    try:
        raise
    except BaseException as observed:
        return observed


class Target:
    def __init__(self, original, replacement):
        self.original = original
        self.replacement = replacement

    @property
    def error(self):
        assert observe_active() is self.original
        raise self.replacement


def run():
    original = E("original")
    nested = F("nested")
    replacement = F("replacement")
    try:
        raise original
    except E:
        assert observe_active() is original
        try:
            raise nested
        except F:
            assert observe_active() is nested
        assert observe_active() is original
        target = Target(original, replacement)
        try:
            value = target.error
        except F as caught:
            assert caught is replacement
            assert caught.__context__ is original
        assert observe_active() is original
    try:
        raise
    except RuntimeError:
        pass
    else:
        assert False
    print("observer identity context")
