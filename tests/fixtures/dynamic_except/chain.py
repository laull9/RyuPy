# 新鲜异常对象；模块层捕获保完整用户帧，避开已登记nested-caught帧旧债。
class Original(Exception):
    pass

class Replacement(Exception):
    pass

class Final(Exception):
    pass

original = Original("original-chain")
replacement = Replacement("replacement-chain")
final = Final("final-chain")

class Targets:
    @property
    def error(self):
        raise replacement

def original_site():
    raise original

targets = Targets()
try:
    try:
        original_site()
    except targets.error:
        print("WRONG.selected")
    except Replacement:
        print("WRONG.same-try-replacement")
except Replacement as caught:
    print("chain.fields", caught is replacement, caught.__context__ is original,
          caught.__cause__ is None, caught.__suppress_context__)
    print("chain.complete", True)
    raise final from caught
