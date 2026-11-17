# 安装文件的多发行共同归属

本门从 ExplorerPro 的原 PySide6 6.9.2 安装布局发现问题：多个发行可在各自 RECORD 中
登记同一安装目标，并实际指向同一个已安装源文件。分发应保留每个发行的完整 metadata、
许可证与归属记录，同时为该目标部署一份载荷。

实现位于 driver 静态收集器，不含第三方库名、`.pyi` 后缀或 Qt 路径特例：

- RECORD 沿既有 Windows 斜杠规则读取并规范点分量；同一发行内部重复目标直接报错。
- 规范相对目标是部署键；真实源路径是源身份。跨发行同目标、同源允许共享；不同源争
  用同目标明确拒绝。每个发行的 `files`、`licenses`、METADATA/WHEEL/RECORD 均保留。
- 全局 `shared_files` 记录目标、真实源和全部 owner；`payloads` 每个目标仅一项。
- 保留安装根内安全符号链接的物化语义。外逃、链接环、缺失及非普通文件继续报错；
  RECORD 越界、版本和 ABI、metadata 完整性检查继续运行。

九组静态合同已纳入 `make python-baseline`，覆盖两组互不相关的发行身份、同目标不同
源冲突、同 owner 重复、规范别名重复、相对路径逃逸、安全/外逃/循环链接及缺失文件。

```sh
python3 -B tests/general_projects/shared_ownership/test_contract.py
python3 -B tests/general_projects/shared_ownership/run.py \
  --ryupy /absolute/path/to/ryupy \
  --python /absolute/path/to/cpython-3.12.13/bin/python3.12
```

产品门使用两组全新独立发行组合。`copper_gate`/`harbor_widget` 共同登记纯 Python
源码和资源；`quartz_relay`/`marsh_engine` 另外共同登记同次编译的真实 CPython C 扩展。
二组发行均有各自独立的 metadata、许可证和成员文件，资源实际采用 site 内安全链接。
应用入口编译为 CIL，第三方依赖由通用 provider 承载；模块、导出与资源没有手工注册规则。

默认分别构建 JIT、NativeAOT standalone、OneFile，共六个产品。每次先运行同次 CPython
oracle，再检查每份发行 metadata/许可和全部 shared owner，确认每个共同目标只部署一次，
链接已物化。构建完成后删除源码、venv、wheel 和全部构建目录，在中文空格路径、空 PATH
及污染 Python 环境执行，比较完整 stdout/stderr/退出码。原生组须实际执行扩展加法得到42。

每组还破坏私有安装副本验证五个构建期负例：重复 RECORD、路径逃逸、缺源文件、缺传递
owner metadata、链接外逃。目录产品进一步验证缺资源或缺原生扩展会在真实运行时报错。
不同源争目标在登记表独立负例验证；同次安装根中同一相对路径只有一个当前真实源身份。

实现前两组合成发行均在旧 CLI 重现重复归属错误，红证为仓外
`~/.cache/ryupy/validation/shared-ownership/20261117T080108.778144Z/`。
最终六产品、十个安装破坏负例和四个运行期破坏负例全部通过，证据为
`~/.cache/ryupy/validation/shared-ownership/20261117T080635.421485Z/`。
日志、provider清单、精简结果及清理记录均在仓外，临时输入和产品由驱动清理。
本轮实测平台为 macOS arm64 / CPython 3.12.13；Windows 原生编译门独立待验。
