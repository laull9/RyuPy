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

2026-11-17 macOS arm64：两版各四输入 × JIT/NativeAOT/OneFile，合计 **24/24** 通过。
四输入为 identity、global_rebinding、builtin_and_function、shutdown；最后一项正文不调用 F，
唯一的 native core 调用来自真实 CP atexit。所有运行均 exit=0、stderr 为空，报告计数归零。
Standalone 的公开入口经原生映像校验，OneFile 私有解包目录在退出后自动删除。

日志名以 `stock-thin-compiler-3.12.13-` / `stock-thin-compiler-3.13.3-` 开头，
本轮运行标识为 `20261117T062000`、`062025`、`062119`、`062533`。
同源完整 make check 为 3424 Runtime pass、3 skip、503 e2e pass、297 pending，Rust/fmt/clippy 全部通过。

长入口循环的临时 owned 保留成本已发现，正在收敛入口登记结构；这些产品证据覆盖正常结束与搬迁。
吞吐、峰值内存保护以及更宽函数体资格继续独立验收，候选默认关闭。
