# 本轮全新边界探针，不计常用CP等价通过。
import subject
def rhs(value):
    raise ValueError("stop")
try:
    subject.fold(5, [3], rhs)
except ValueError as exc:
    tb = exc.__traceback__
    if tb is None:
        print("traceback-unavailable")
    while tb is not None:
        if tb.tb_frame.f_code.co_name == "fold":
            print("total", tb.tb_frame.f_locals["total"])
        tb = tb.tb_next
