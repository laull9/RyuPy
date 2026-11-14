# CPython Provider 固定 20 项目验收矩阵与驱动规范

本文档记录 RyuPy 基于固定 CPython 3.12.13 (PBS 20260623) Provider 架构对 `tests/projects/manifest.tsv` 中固定 20 个纯 Python 项目的完整实测矩阵、验收驱动实现与语义边界分析。

---

## 1. 验收驱动与隔离模型架构

测试驱动由独占文件 [`tests/cpython/projects_runner.py`](projects_runner.py) 与 [`tests/cpython/projects_support.py`](projects_support.py) 实现：

1. **原版原入口与独立业务输入**：
   - 严格保留 `tests/projects/manifest.tsv` 与 `lock.tsv` 中锁定的原始版本与 `entries/<id>_sample.py` 业务入口；
   - 不修改 manifest/lock，不篡改业务输入，不跳过任何困难项目。
2. **同次 Oracle 即时生成与独立 cwd**：
   - 每次测试同次调用固定 CPython 3.12.13 解释器生成预期 Oracle 输出；
   - 每次执行（Oracle、RyuPy JIT、AOT、Reloc）均分配独立外部临时 cwd，杜绝进程间文件副作用污染（例如 `binaryornot` 现场创建的样本文件）。
3. **干净私有发行构建与仓库外清理**：
   - 在仓库外临时目录构建隔离的私有 CPython 发行目录，排除基础发行site-packages中的开发工具，仅部署本次固定项目载荷；
   - 测试产物仅放置于仓库外临时目录（如 `/private/tmp/`），并在进程退出时通过 `try...finally` 自动清理，源码仓库内零污染。
4. **主代理依赖闭包接入点**：
   - `projects_support.py` 显式导出 `register_dependency_resolver(hook)` 函数，允许主代理将通用自动依赖闭包无缝注入，无需重复实现解析逻辑。

---

## 2. 真实 20 项目实测矩阵（JIT 模式）

实测环境：macOS arm64, .NET 10.0, CPython 3.12.13。
验证标准：退出码为0；stdout、stderr和文件副作用均与同次CPython比较。仅对已知载荷警告头的绝对路径做相对名规范化，保留警告类型、行号及正文。

| 序号 | 项目 ID | 类别 | 版本 | 运行模式 | 退出码 | Oracle 对齐 | 判定结果 | 现场备注 |
| :--- | :--- | :--- | :--- | :--- | :---: | :---: | :---: | :--- |
| 01 | `six` | 通用库 | 1.16.0 | JIT | 0 | YES | **PASS** | 类型兼容助手正常运行 |
| 02 | `sortedcontainers` | 数据结构 | 2.4.0 | JIT | 0 | YES | **PASS** | SortedList/Dict/Set 基本操作全部对齐 |
| 03 | `toml` | 配置解析 | 0.10.2 | JIT | 0 | YES | **PASS** | loads/dumps 往返一致 |
| 04 | `tomli` | 配置解析 | 2.0.1 | JIT | 0 | YES | **PASS** | 基础类型与嵌套数组表解析正常 |
| 05 | `pyparsing` | 解析 | 3.1.2 | JIT | 0 | YES | **PASS** | 语法解析动作与命名提取对齐 |
| 06 | `attrs` | 数据类 | 23.2.0 | JIT | 1 | NO | **RUNTIME_FAIL** | 缺少模块活字典支持（见下节分析） |
| 07 | `networkx` | 图算法 | 3.3 | JIT | 0 | YES | **PASS** | 图拓扑增删边与最短路径完全一致 |
| 08 | `python-dateutil` | 日期 | 2.9.0.post0 | JIT | 0 | YES | **PASS** | 传递依赖 six 联动，日期计算一致 |
| 09 | `click` | 命令行工具 | 8.1.7 | JIT | 0 | YES | **PASS** | 命令选项解析与上下文调度一致 |
| 10 | `docopt` | 命令行工具 | 0.6.2 | JIT | 0 | YES | **PASS** | usage 模式匹配与默认值一致 |
| 11 | `pyfiglet` | 命令行工具 | 1.0.2 | JIT | 0 | YES | **PASS** | ASCII 艺术字渲染与字体查找一致 |
| 12 | `tabulate` | 文本处理 | 0.9.0 | JIT | 0 | YES | **PASS** | 传递依赖 wcwidth 联动，表格对齐一致 |
| 13 | `texttable` | 文本处理 | 1.7.0 | JIT | 0 | YES | **PASS** | 传递依赖 wcwidth 联动，多行边框对齐 |
| 14 | `markdown` | 文本处理 | 3.5.2 | JIT | 0 | YES | **PASS** | 携带 egg-info 发现 entry points 扩展 |
| 15 | `chardet` | 编码检测 | 5.2.0 | JIT | 0 | YES | **PASS** | 文本编码识别与置信度完全对齐 |
| 16 | `wcwidth` | 终端自动化 | 0.2.13 | JIT | 0 | YES | **PASS** | 宽字符与组合字符宽度计算完全对齐 |
| 17 | `colorama` | 终端自动化 | 0.4.6 | JIT | 0 | YES | **PASS** | ANSI 样式常量与初始化行为对齐 |
| 18 | `termcolor` | 终端自动化 | 2.4.0 | JIT | 0 | YES | **PASS** | colored 属性高亮组合输出一致 |
| 19 | `binaryornot` | 文件自动化 | 0.4.4 | JIT | 0 | YES | **PASS** | 传递依赖 chardet，二进制判定一致 |
| 20 | `progress` | 终端自动化 | 1.6 | JIT | 0 | YES | **PASS** | 进度条迭代计数与步进一致 |

**总结**：全量 20 项中，**19 项严格通过（通过率 95%）**，1 项明确预期运行时阻断。

---

## 3. 失败项语义阻断深度分析（attrs）

### 真实报错现场
```text
Traceback (most recent call last):
  [RyuPy 编译栈]
  File "attrs_sample.py", line 5, in <module>
  [CPython 解释栈]
  File ".../attr/_next_gen.py", line 153, in define
  ...
  File ".../attr/_make.py", line 2172, in _make_init
AttributeError: module '__main__' has no attribute '__dict__'
```

### 阻断原因剖析
- `attrs` 在生成数据类的初始化构造器（`_make_init`）时，需要反射调用类所在定义模块的全局命名空间字典：`sys.modules[cls.__module__].__dict__`，并在该字典中注入生成的代码对象。
- 拒绝临时快照替身：RyuPy 运行时若仅返回每次新构造的 `new PyDict()` 浅拷贝快照，虽然能绕过读取，但由于缺少与编译器全局槽（CIL 字段）的双向实时同步（活字典 Live Dict 合同），会导致后续属性写入无法真正生效且破坏对象身份一致性（`m.__dict__ is m.__dict__` 为 False）。
- 依照 ADR 与工程准则，RyuPy 不为通过单一样例引入伪语义，将此能力缺口如实记录为当前阶段明确阻断。

---

## 4. 主代理完整三模式复验（2026-11-14）

主代理修正驱动后运行全部20项目的JIT、NativeAOT及搬迁，共60个执行臂。
每臂使用独立cwd和私有输入，搬迁臂删除私有源码、基础发行及构建目录后，
以空PATH直接执行NativeAOT产品；宿主报告检查加载文件均来自产品目录。

| 模式 | 通过 | 失败 | 失败项目 |
| --- | ---: | ---: | --- |
| JIT | 19/20 | 1 | attrs |
| NativeAOT | 19/20 | 1 | attrs |
| 输入删除搬迁 | 19/20 | 1 | attrs |

完整首轮保留全部原始stderr，docopt因四条SyntaxWarning的绝对路径不同记为失败；
随后增加严格限定于已知载荷警告头的路径映射，单独重跑docopt三模式全部通过。
上表为完整首轮与该项重跑合并结果。attrs三模式均明确失败于编译入口模块缺少活字典；
驱动遇到该已知失败仍返回非零，不设置失败豁免。

原始JSON及日志写仓库外，人工结论在本文登记。其他平台的固定20项目矩阵继续待验。

---

## 5. 验收命令使用说明

```bash
# 运行全量 20 项目 JIT 验证，输出至仓库外 JSON
python3 tests/cpython/projects_runner.py --mode jit --output-json /private/tmp/ryupy_jit_results.json

# 运行单个项目（如 six）的 AOT 模式
python3 tests/cpython/projects_runner.py --mode aot --project six

# 运行单个项目（如 six）的跨目录搬迁与原生边审计
python3 tests/cpython/projects_runner.py --mode reloc --project six
```
