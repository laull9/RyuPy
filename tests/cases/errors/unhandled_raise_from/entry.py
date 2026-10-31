# M2.2：`raise ... from ...` 的显式异常链（未捕获路径）。
cause = ValueError("source")
raise RuntimeError("outer") from cause
