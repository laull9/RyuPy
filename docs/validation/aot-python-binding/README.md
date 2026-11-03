# 固定 Python C ABI 绑定：三平台有限部署验证

本次重建提交 `7713113175d15f0bea9f64e22d05f88f107e657f`，
[绑定 CI 41560771202](https://github.com/laull9/RyuPy/actions/runs/41560771202)。
它与本地已验证 `1ea1a3c9a96819ca202b74361c360a760f177dd9` 的 Git tree
`a2b99e547b2f56a87fb4f935fe2c1deab4392321` 完全相同；通过已授权 GitHub connector
交付时仅提交元数据不同。旧工作区四个未推提交没有被恢复，本轮重新实现并重新验证。
当前仍为 WIP，未合入 main。

## 交付范围

公开构建入口 `ryupy build app.py --native-binding binding.json` 生成普通可导入模块和
一等函数。当前只支持一个声明模块、一个 `cdecl (i32,i32)->i64` 导出，
不是 ctypes、CPython 扩展 ABI 或任意 wheel。完整声明格式见
[构建合同](../../../tests/aot/cabi_probe/PYTHON_BINDING_BUILD.md)。

- JIT 包使用 `_internal/native/v1/b0/` 的实际 shared 库；首次调用经 owner 与逻辑库名
  精确匹配的 resolver 加载，不能退回默认裸名搜索。
- AOT 使用受信自有静态 archive 和精确 DirectPInvoke，搬迁 native 不需要该 shared。
- schema3 区分真实源码、生成模块、声明和选中模式 native 输入，绑定实际文件哈希。
- 普通 import、保存函数身份、模块重绑、求值顺序、严格参数类型/范围/arity 及异常帧均在真实应用验证。
  `bool`/非整数与越界拒绝，不调用 `__index__`；错误 ABI 本身仍可能崩溃。

## 固定验证

每 host 一次 JIT build、一次 AOT publish；Windows/macOS 各 9 次应用，Linux 10 次
（额外一次无 SDK/Python 容器），另各一次自有 C reference 和 CPython 数值参考。
SDK 10.0.403，CPython 3.12.13；1800 秒累计预算，首失败即停，无性能样本。
成功模式按同平台原始双流比较，错误模式核完整两帧、行号、函数和异常类型/逻辑诊断。

| 实际宿主 | 应用次数 | 裸 native 字节 | 绑定阶段秒数 |
| --- | ---: | ---: | ---: |
| Linux x64 glibc | 10 | 4,633,952 | 17.965 |
| macOS 14.8.9 arm64 | 9 | 4,438,824 | 13.145 |
| Windows x64 MSVC | 9 | 4,591,104 | 29.610 |

时间只是一轮构建/验证观察，不是吞吐、冷启动或平台性能比较。逐模式输出哈希、
真实产物清单和保留限制见 [summary.json](summary.json)。

缺库、关闭动态代码能力后缺库、缺符号及坏格式均使用独立副本；原始发布清单与产物保留，
损坏副本无法通过 exactset/hash 校验。缺库的搜索诱饵不改变系统环境。
Linux 明确设置 LD_LIBRARY_PATH；macOS 仅记录请求 DYLD_LIBRARY_PATH，未证明宿主 loader
实际保留该变量，因此不把 macOS 缺库结果冒充可搜索诱饵验证。

## 保留边界与负结果

- macOS 实际宿主 14.8.9。发布日志有真实 linker 警告：自有 `probe.o` 按 macOS 14.0 构建，
  NativeAOT 链接目标为 12.0。本轮不声称零警告或支持 macOS 12 部署；后续需对齐最低系统版本合同。
- 原源码/输出目录改名为构建证据而非删除；Mac/Windows 仍安装 SDK，不是无 SDK 干净机器。
  Linux 额外容器只挂裸 native、只读且禁网，并确认无 dotnet/Python。
- 系统动态库仍是依赖；未实现第三方传递依赖自动收集、任意嵌入 host/ALC、资源与 binding 组合。
- 不包含字符串/指针/buffer/回调/所有权/errno、`.pyd`/PyObject/GIL。
- 本轮没有新的性能测量。[原固定 36 成本](../aot-module-call-pair-cost/README.md)中的
  禁 tier matmul 约 23% 回退与模块调用约 32MB 增量仍开放；薄 wrapper 小修不代表性能问题关闭。

新环境本地完整门禁：546 C#、228 固定 CP 基线、202 e2e + 30 ignored；
另外 9 PE / 14 driver / 2 CLI 失败生命周期 / 11 工具专项通过。
同提交已有 Linux smoke/coverage/resources、Mac/Windows host 和旧 C ABI 机制 CI 全部 success：
[41560771141](https://github.com/laull9/RyuPy/actions/runs/41560771141)、
[41560771096](https://github.com/laull9/RyuPy/actions/runs/41560771096)、
[41560771139](https://github.com/laull9/RyuPy/actions/runs/41560771139)。
原始日志、构建二进制仅在 CI artifact；本目录仅保精简结论与哈希。

## 后续 macOS 自有测试输入目标修复

提交 `ab40df68918a08897093caa331f64f4adf99b814` 仅对自有 C fixture 的五条编译/链接命令
设置 minimum 12.0，生产driver与用户archive不改。
[绑定复验41563083923](https://github.com/laull9/RyuPy/actions/runs/41563083923)与
[既有机制41563083954](https://github.com/laull9/RyuPy/actions/runs/41563083954)均三host success。
Mac原9应用、完整错误帧、清单/源/产物hash与实际ILC已重新独立核验；
直接解析原始Mach-O load commands，独立object、archive成员、裸native均为
`LC_BUILD_VERSION / macOS / minimum12.0.0 / SDK14.5.0`。同次SDK双流中的原14→12警告消失，
不是仅看编译命令参数。详见[精简记录](macos-target.json)。

实际宿主仍为macOS14.8.9，未运行macOS12机器；该修复不宣称任意用户静态库满足相同最低版本。
前述771警告保留为历史真实结果，不回写成从未发生。Linux/Windows与旧机制本轮确认的是
官方job成功，不冒重新逐原始artifact核全套。未新增性能样本，原成本负项仍开放。
