# M2.2：未捕获异常保留显式 cause，不能只看 stdout。
try:
    raise ValueError("source")
except ValueError as error:
    raise RuntimeError("outer") from error
