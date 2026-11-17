# 真实 Python 薄函数产品验收

本门调用 Rust CLI 编译手写 Python，验证实际 CIL 业务体和程序正常关闭；
模块保留函数至退出，既不主动 `del f`，也不提前清模块字典。
资格是显式 `--experimental-stock-thin-functions`，固定 stock CPython 3.12.13 / 3.13.3，默认关闭。

先由总控完成 Runtime/provider 与 CLI 构建，再执行：

```bash
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.12 --mode jit
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.13 --mode standalone
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.12 --mode onefile
```

`--case identity` 可用于首轮短验证；`--mode all` 串行运行三形态。
每格先运行指定 CPython 即时 oracle，随后编译、物理删除输入源码/venv/构建中间目录，
搬迁至中文空格路径。执行时 PATH 为空，PYTHONHOME/PYTHONPATH 指向不存在目录。
JIT 使用绝对路径 dotnet；Standalone/OneFile 直接执行发布的原生入口。

验收比较完整 stdout、stderr、退出码，读取真实 Host 报告，要求专用程序权限已使用、
native/core/owned-H/ledger/queue 全零且没有 finalize 后 DECREF。
原生调用及 owner 析构必须发生。OneFile 额外要求从本轮私有临时目录加载且自动清理解包目录。

成功和失败日志保存在仓外 `~/.cache/ryupy/validation/adr0208-20261117/`，
所有临时产品在成功、失败或超时后清理。驱动无需普通文件摘要。
本门与独立 Host Runner、固定二十项目兼容矩阵分别报告；三组证据分别维护。

当前驱动已落地，正常关闭接线及完整产品运行结果由总控统一验收后更新。
