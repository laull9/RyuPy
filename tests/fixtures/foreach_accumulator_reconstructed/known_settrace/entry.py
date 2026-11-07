# 本轮全新边界探针，不计常用CP等价通过。
import subject
import sys
seen = []
def trace(frame, event, arg):
    if event == "call" and frame.f_code.co_name == "fold":
        seen.append("fold")
    return trace
try:
    sys.settrace(trace)
except AttributeError:
    print("settrace-unavailable")
    print(subject.fold(0, [1, 2]))
else:
    print(subject.fold(0, [1, 2]))
    sys.settrace(None)
    print(seen)
