# ADR-0095：codecs 的有限 BOM 常量族

日期2026-11-04。状态：独立源码审、首次专项与正常完整门禁通过；不代表完整 codecs 或 chardet 通过。

## 背景与范围

ADR0094 已保存锁定 chardet 入口的真实运行失败：缺少 codecs，原流无帧。源码调查表明该包使用 BOM 常量，但不能据此预判后续执行结果。本切提供通用的公开常量族，不按包名或输入特判，也不导出空编码器。

对齐固定 CPython 3.12.13 的十四个名称、五个不可变 bytes 对象：BOM_UTF8；BOM_UTF16_LE/BE；BOM_UTF32_LE/BE。BOM_LE/BE、BOM32_LE/BE 分别与 UTF16 对象同一，BOM64_LE/BE 与 UTF32 同一。BOM 与 BOM_UTF16 指运行机器的 UTF16 对象，BOM_UTF32 指运行机器的 UTF32 对象。历史名称 BOM32 不能误解为 UTF32。

端序采用 BitConverter.IsLittleEndian，与现有 sys.byteorder 来源一致，不依据编译机器冻结。模块成员可按既有机制重绑定，仅改变该槽；其它别名和先前 from-import 的对象引用不联动。真实同名源模块沿原解析和注册机制处理。

encode/decode/lookup、CodecInfo、registry、错误处理器、增量和流接口均不在本切，不导出占位。缺少成员沿既有 AttributeError/ImportError。UTF8 encode/decode/lookup 的合法 CP 正例保留 pending，不能把这些 API 改写成 CP 也缺失。

## 性能与后续设计

新增成本仅初始化冷路的五个 PyBytes 对象、十四个字典条目及普通模块读取；不修改字符串、bytes、bytearray、IO 编解码或前缀热路。没有进行性能测量或宣称提速。两端序工厂单测只是分支覆盖，不等于在另一架构执行。

后续编码接口必须另行定义文本/二进制输入输出域、错误对象、registry 生命周期和状态编码器。当前编码工具的 CLR fallback、errors 参数及缓冲协议并不自动满足 codecs 合同，不能直接包装后宣称完整兼容。

## 验证计划与证据边界

四项运行时用例覆盖两端序、全部值和别名身份、正常模块缓存与重绑定及未实现成员。两项 CP-first/JIT 应用验证真实公共使用和同名源模块。源码遮蔽的 CP 参考明确先禁 frozen_modules、移除启动预载的 codecs，再把真实源目录置首；这是受控先决条件，不声称 CP 默认启动时已经具有该查找状态。未修改应用源来伪造结果。

固定参考单进程上限30秒，保脚本、解释器和原流哈希；应用测试保双方完整 stdout/stderr/exit。专项及正常完整门禁结果见末段。没有新项目、AOT、性能或跨平台验收，不更新历史5/20。

首次参考 exit0、stderr为空，B1/B2全部标签齐全；随后首次四项运行时与两项CP/JIT差分通过，当时完整门禁进行中。

随后首次正常 make check exit0：fmt、严格clippy、workspace、1651项C#零失败跳过、固定228项CP基线、202项e2e及原30 ignored通过。日志SHA256 `c661091be5800bad3c6d0c9d85cd0982291d5441ec733ae78a30dd693392c7ff`。该门禁阶段候选未公开，尚未重跑chardet或其它项目，不新增AOT/性能结论。

## 后续唯一锁定 chardet 入口

候选 `c41477b070132e43bd2ea1230fabd4af6181b57f`（tree `57cd11b3870571e74d1e56ab2b5e244e42727707`）单次CLI准备exit0，0.164843秒。锁定官方chardet5.2.0归档与501源码文件、原入口和19字节golden不变；保30/90/30秒阶段和180秒总限，首失败停，不启用阶段诊断。

CP一次exit0、stdout精确golden且stderr空；编译一次exit0，1.619345秒，精确产出程序DLL/runtime DLL/runtimeconfig。JIT一次exit1、0.565812秒，stdout空；全窗口2.402095秒，无timeout。原stderr真实帧为 `chardet_sample.py:4` → `__init__.py:49 detect` → `universaldetector.py:220 feed`，末尾 `NotImplementedError: re: mutable or view regex subject is not supported`。

这证明本次运行越过缺少codecs，抵达现有可变正则subject拒绝；不是chardet通过，也不能据此宣称整个编码接口或其后执行路径已闭合。结果SHA256 `bd6ab3bfe2cd34e1c40aebe4957b00e4cdea1e6f4fe5f7710ddcd25ca975ec64`，JIT stderr SHA256 `7c0710120769e1a307f5c0a09c9d9a6e6119fc55307e15130e18156fddde72a3`。候选未发布，不更新历史5/20，不冒main、全库、AOT或性能结论。
