# RyuPy 当前项目入口支持测试

测试日期：2026-11-05。稳定提交：`e066238929318cf51607a8dbad056be224a43f29`。

**默认配置：9/20 通过，9项编译未支持，2项运行失败。没有跳过或超时。**
为 pyfiglet 使用已支持、已文档化的 `--include-module pyfiglet.fonts` 后，其原入口也通过；带这项显式配置可通过10/20入口，不能把它算作默认配置通过。

## 逐项结果

| 项目 | 固定版本 | 默认结果 | 首个阻塞或说明 |
|---|---|---|---|
| six | 1.16.0 | 编译未支持 | `six.py:87`，`__import__` 未实现 |
| sortedcontainers | 2.4.0 | 编译未支持 | `sorteddict.py:407`，类体内嵌套 class |
| toml | 0.10.2 | 编译未支持 | `decoder.py:644`，函数内动态 class |
| tomli | 2.0.1 | 通过 | 原入口输出一致 |
| pyparsing | 3.1.2 | 编译未支持 | `__init__.py:129`，`from ... import *` |
| attrs | 23.2.0 | 编译未支持 | `attr/_make.py:67`，Enum 的 `enum.auto()` 值 |
| networkx | 3.3 | 编译未支持 | `__init__.py:17`，`from ... import *` |
| python-dateutil | 2.9.0.post0 | 编译未支持 | `__init__.py:24`，`globals` 未实现 |
| click | 8.1.7 | 编译未支持 | `_termui_impl.py:37`，`t.Generic[V]` 基类表达式 |
| docopt | 0.6.2 | 运行失败 | `docopt.py:466`，`re.split` 缺失 |
| pyfiglet | 1.0.2 | 默认运行失败；显式配置通过 | 默认未包含 `pyfiglet.fonts`；上述字体选项补充验证通过 |
| tabulate | 0.9.0 | 通过 | 原入口输出一致 |
| texttable | 1.7.0 | 通过 | 原入口输出一致 |
| markdown | 3.5.2 | 编译未支持 | `util.py:272`，`Generic[_T]` 基类表达式 |
| chardet | 5.2.0 | 通过 | 原入口输出一致 |
| wcwidth | 0.2.13 | 通过 | 原入口输出一致 |
| colorama | 0.4.6 | 通过 | 原入口输出一致 |
| termcolor | 2.4.0 | 通过 | 原入口输出一致 |
| binaryornot | 0.4.4 | 通过 | 原入口输出一致；声明依赖 chardet 5.2.0 |
| progress | 1.6 | 通过 | 原入口输出一致 |

## 测试口径

- 使用同一稳定提交的冻结 CLI 和 runtime，未混入尚未提交的 re.split 候选。
- 20项原入口、原 golden 和官方锁定源码均未修改。官方归档 SHA256 与锁文件一致，3187个源码及资源文件在运行前后逐字节哈希验证。
- 固定 CPython 3.12.13 先运行，20/20退出成功且 stdout 与原 golden 完全一致；再编译，11/20成功；这些项目实际运行，9/11成功且 stdout 与 CP/golden 完全一致。
- 每个阶段保存完整 stdout、stderr、退出码、耗时、命令及哈希。CP的原警告不会被隐藏，也不会把编译成功误计为运行通过。
- 保持既有默认项目清单的搜索范围：CP可见全部固定项目源码，编译仅可见本项及清单声明依赖。python-dateutil 的 six 依赖未被清单声明，本轮没有偷偷补上；当前更早在 globals 处编译失败。
- pyfiglet补充检查只增加已文档化的字体模块选项，原入口、字体文件、输出期望和稳定工具不变。其编译、JIT均成功，stdout一致且JIT stderr为空。
- 这是固定 Linux JIT **入口样例**支持结果，不代表完整上游测试、所有库 API、Windows或AOT通过。分母固定为20，不因失败而缩小。

## 后续

继续通用 re.split，解除 docopt 当前运行阻塞；后续仍须按真实语义、性能与完整门禁验证，不能针对单个项目特判。
