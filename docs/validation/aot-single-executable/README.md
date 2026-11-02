# 无资源产物：仅可执行文件搬迁验证

实测工具提交 `a2560009c666a4be1256afe1d830abd60fa8cfc3`；没有修改编译器/runtime，
没有额外 ILC 或性能采样。每平台从原一次无资源 smoke 发布复制相同 native 字节。

- [Linux x64 glibc](https://github.com/laull9/RyuPy/actions/runs/41454408111)
- [macOS arm64 / Windows x64 MSVC](https://github.com/laull9/RyuPy/actions/runs/41454408061)

三平台新目录在运行前后均恰一个普通可执行文件，无manifest、调试符号、托管DLL或
资源。复制前后与原完整发布清单SHA一致；原源码/IL已删除。原 all、argv、exit、error
四模式实际双流按原判据比较，退出码为0/0/7/1；完整异常帧/行/函数未删除或放宽。

| 平台 | 原生文件字节 | 环境证据 |
| --- | ---: | --- |
| Linux x64 | 4,633,568 | 只挂单exe目录到无dotnet/Python容器；禁网、只读；原完整树不可见 |
| macOS arm64 | 4,455,320 | 同机新目录、restricted环境；SDK和其它host目录仍可见 |
| Windows x64 | 4,591,104 | 同机新目录、restricted环境；SDK和其它host目录仍可见 |

精简逐平台摘要见 `summary.json`；原命令/双流/二进制保上述artifact（14天），不入Git。
原完整目录清单、符号和资源验收仍保留，不因可执行文件可运行就删除审计证据。

## 这说明什么、没有说明什么

已有这类**无显式资源**程序可在没有相邻应用sidecar的目录执行。Linux容器进一步证明
不需要原构建树、Python或dotnet；Mac/Windows不是无SDK干净目标机，也未证明任意host
路径都不可访问。仍依赖系统libc/系统框架/UCRT等，不能叫绝对零依赖静态二进制。

这不是任意项目的onefile打包器。显式资源bundle仍需搬迁完整 `_internal/resources/v1`
树；第三方原生依赖静态链接或外置 `_internal` 尚是独立后续切。不会为了单文件形式
强加自解包或假装PublishSingleFile与NativeAOT自动兼容。N5父目标仍未完成。

工具轻测14项中10通过/4平台限定跳过；固定CP13/JIT prepare-only通过不曾被当成ILC。
真实结论来自以上三平台新单exe运行；没有性能收益或大小优化结论。
