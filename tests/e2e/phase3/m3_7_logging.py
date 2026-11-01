# 日志过滤、父链和用户流独立验收，不依赖 argparse。
import io
import logging
out = io.StringIO()
h = logging.StreamHandler(out)
h.setFormatter(logging.Formatter("%(levelname)s:%(message)s"))
log = logging.getLogger("fixture.child")
log.handlers.clear()
log.propagate = False
log.setLevel(logging.INFO)
log.addHandler(h)
class Bad:
    def __str__(self):
        raise ValueError("must not format")
log.debug(Bad())
log.info("count=%d %s %%", 3, "cat")
log.warning("%(name)r", {"name": "猫"})
print(out.getvalue(), end="")
print(log is logging.getLogger("fixture.child"))
parent = logging.getLogger("fixture")
parent.setLevel(logging.CRITICAL)
parent.addHandler(h)
parent.propagate = False
log.removeHandler(h)
log.propagate = True
log.info("parent handler")
print(out.getvalue().endswith("INFO:parent handler\n"))
h.close()
print(out.closed)
# 必需msg/重复绑定先于等级过滤；空Formatter格式恢复默认message格式。
h.setFormatter(logging.Formatter(""))
log.info(msg="keyword")
print(out.getvalue().endswith("keyword\n"))
try:
    log.debug()
except TypeError:
    print("missing message")
try:
    log.debug("one", msg="two")
except TypeError:
    print("duplicate message")
for invalid in ["literal", "%s", "%%", "%(message)q"]:
    try:
        logging.Formatter(invalid)
    except ValueError:
        print("invalid formatter")
