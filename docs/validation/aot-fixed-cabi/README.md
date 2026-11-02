# 固定 C ABI 机制：三平台真实调用与静态链接

验证提交 `b89cc70e76e40e9faef50d877841c4839968a887`，
[真实 CI 41460349000](https://github.com/laull9/RyuPy/actions/runs/41460349000)。
这是测试工具原型，不是新增公开 Python FFI、CLI 配置或任意 wheel 支持。

## 已验证的最小链路

同一份自有 C 源导出 `int64_t ryupy_demo_add_i32(int32_t, int32_t)`，先扩宽后相加。
现有 CIL 后端的 workspace example 发射真实 PInvoke，并对同一 PE 读回核验
Cdecl、ExactSpelling、PreserveSig、无方法体、ModuleRef 和 Main 五个静态可达调用。
JIT 使用同源码共享库；NativeAOT 使用真实对象 archive 与精确 DirectPInvoke/NativeLibrary，
不添加全程序集 root，不用 CPython ABI。

| 实际宿主 | C reference / JIT shared / AOT static | 原生文件字节 | 全过程秒数 |
| --- | --- | ---: | ---: |
| Linux x64 glibc | 各一次通过 | 3,721,552 | 39.953 |
| macOS arm64 | 各一次通过 | 3,214,608 | 75.720 |
| Windows x64 MSVC | 各一次通过 | 1,108,992 | 112.047 |

每次输出严格为五行 `0 / 3 / 4294967294 / -4294967296 / -1`；平台自身换行字节
逐字一致，均 exit 0、stderr 空。构建耗时是该 CI 单次端到端观察，不是性能比较。
每 host 1800 秒累计预算，只有一次 AOT publish；SDK 为 10.0.403。

## 静态与搬迁证据

- SDK 实际执行 ILC；共享库、原 IL、源码、宿主及构建目录删除后，运行搬迁 native。
- archive 中恰有一个本次 C 对象，记录对象头、符号及 hash；Windows 不拿 importlib 代替静态库。
- 最终依赖表无 probe 共享库；静态 archive、实际链接、删除共享库后运行共同支撑静态机制结论。
  不要求最终可执行文件导出 C 符号，也不把有限 archive 检查冒任意 `.lib` 认证。
- 原 DLL 按合同删除：保存的是实际 emitter PE readback 日志及 hash，不声称归档后再次解析原 DLL。
- 保留简表与 [summary.json](summary.json)；原始日志、archive 与 native 在 CI artifact，未提交 Git。

## 保留的失败与边界

首轮 [41458855288](https://github.com/laull9/RyuPy/actions/runs/41458855288)，提交 `9ea96b3`：
Linux 机制通过；Mac `ar -V` exit 1 只有 usage；Win `lib /?` exit 1100 有正常版本帮助。
两者均在 C 编译前停止。修复只改变工具版本取证：Mac 实际 xcrun ar 身份/hash 与 Xcode
版本，Win 仅已证帮助退出码加数字版本横幅；原五值、静态链接和搬迁判据不变。
纯工具修后 7/7 通过；此前完整语言门禁 523 C# / 228 CP 基线 / 202 e2e + 30 ignored。

三个宿主仍安装 SDK，仍依赖操作系统库，不是干净目标机或绝对无外部依赖的证明。
JIT 仅验证同程序集目录的共享库加载，未实现 `_internal` 原生库收集/安全 resolver。
公开 Python 参数转换、异常映射、动态重绑与声明式绑定仍需独立设计和验收。
指针、缓冲区、UTF-8、所有权、回调、errno、任意 `.pyd`/wheel、PyObject/GIL 均不在本结论。
