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
            try:
                print("total", tb.tb_frame.f_locals["total"])
            except NotImplementedError as boundary:
                # 已找到真实fold帧；只登记尚未开放的locals，退回None不满足新边界。
                if str(boundary) != "native frame field f_locals is not supported":
                    raise
                print("traceback-locals-unavailable")
        tb = tb.tb_next
