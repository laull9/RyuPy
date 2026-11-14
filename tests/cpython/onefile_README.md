# RyuPy AOT 单文件（--onefile）独立交付真实验收规范

本文档定义 RyuPy NativeAOT 单文件发布的架构契约、测试矩阵、并发及负例边界。
所有实现由 Codex（driver `onefile` + `runtime/RyuPy.OneFile`）与主代理（CLI 接线）负责；
验收套件与驱动由测试子代理独占维护于 `tests/cpython/onefile*.py` 及 `tests/cpython/fixtures/onefile_*`。

---

## 1. 用户语义与 CLI 契约

用户期望直接将 Python 应用打包为单一可分发原生二进制文件，无需部署任何外部目录或解释器：

```bash
# 预期标准命令行（由主代理完成 CLI 接线）
ryupy build app.py --onefile --out-dir dist/
# 或与 --aot 组合
ryupy build app.py --aot --onefile
```

### 交付物物理形态保证
1. **单一可执行文件**：发布目录中只存在唯一二进制文件（Windows 上为 `app.exe`，POSIX 上为 `app`）。
2. **构建输入清理门**：发布完成后，测试驱动将**物理删除**全部构建输入（包括所有 `.dist`、中间 `.build`、原始源码、虚拟环境 `venv`、基础 Python 发行及 wheel），确保运行时没有任何外部源码或宿主目录依赖。
3. **中文与空格路径搬迁**：将唯一的单文件二进制搬迁至包含中文全角字符与空格的全新路径（例如 `工作区/独立搬迁 目录 & 空格路径/业务应用`）。

---

## 2. 运行时隔离与环境污染合同

单文件执行时必须在极端隔离与受污染环境下验证：
1. **清洁 PATH**：`PATH` 环境变量仅指向一个绝对为空的目录 `empty-path`，禁止按命令名寻找外部 `python`、`python3` 或 `dotnet` 工具。
2. **受污染 PYTHONHOME / PYTHONPATH**：
   - POSIX: `PYTHONHOME=/污染环境/不存在的Python`，`PYTHONPATH=/污染环境/虚假模块包`
   - Windows: `PYTHONHOME=C:\污染环境\不存在的Python`，`PYTHONPATH=C:\污染环境\虚假模块包`
3. **独立工作目录**：以全新且独立的 `cwd` 启动，业务应用在此生成文件副作用，不得污染构建或源码目录。

---

## 3. 手写业务应用覆盖范围（`fixtures/onefile_app/app.py`）

手写业务测试应用涵盖以下完整核心能力：
1. **SQLite3 数据库**：真实创建 SQLite 数据库、数据表、参数化插入及聚合查询（SUM/COUNT）。
2. **SSL / 密码学**：`ssl.create_default_context()` 上下文初始化及 SHA-256 算法测试。
3. **元数据读取**：`importlib.metadata.version('onefile_pkg')` 与 `metadata('onefile_pkg')` 正常读取。
4. **内嵌包资源**：`importlib.resources.files('onefile_pkg').joinpath('资源.txt')` 读取包含中文与特殊标点的 UTF-8 资源。
5. **默认 UTF-8 编码**：全流程文件读写默认 UTF-8。
6. **参数传递与三流**：
   - 包含普通参数、空字符串参数 `""`、含换行符参数 `"含换行\n参数"`。
   - 标准输入（`sys.stdin`）流式读取。
   - 标准输出（`sys.stdout`）业务打印。
   - 标准错误（`sys.stderr`）生命周期诊断。
7. **精确非零退出码**：`--exit-code 42` 可使进程精确返回退出码 42。
8. **文件副作用**：在当前工作目录下输出 `业务副作用_onefile.txt`。

---

## 4. 并发、生命周期与负例校验

1. **双进程并发运行**：同时拉起 2 个单文件执行实例，验证单文件的自解压/运行时缓存互斥锁不发生死锁或写入冲突。
2. **中断与清理**：单文件自解压缓存应具备幂等性与租约保护，支持安全清理。
3. **损坏单文件负例**：
   - 截断或破坏单文件二进制末尾元数据（例如最后 512 字节 Payload 索引）。
   - 启动被破坏的单文件必须立即以非 0 退出码失败，并向 stderr 报告错误，绝不能发生未定义段错误（Segmentation Fault）或静默成功。

---

## 5. 多平台与容器验收

| 平台 | Target RID | 二进制格式 | 容器验收要求 |
| --- | --- | --- | --- |
| macOS 15 | `osx-arm64` | Mach-O 胖/单架构单文件 | 宿主原生运行，Mach-O 加载边审计 |
| Windows 2022 | `win-x64` | PE / COFF 单文件 | 宿主原生运行，PE 结构审计 |
| Ubuntu 24.04 | `linux-x64` | ELF64 单文件 | 宿主原生运行 + `runtime-deps:10.0-noble` 纯净容器（无 Python/.NET/SDK）|

---

## 6. 测试驱动重跑命令

```bash
# 检查接口与前置状态
python3 -B tests/cpython/onefile_run.py --check-interface

# 完整执行（依赖已编译的 ryupy-cli 与已接线的 --onefile）
python3 -B tests/cpython/onefile_run.py --ryupy target/debug/ryupy

# 调试时保留临时目录
python3 -B tests/cpython/onefile_run.py --keep
```
