# 首次真实CLI NativeAOT发布与搬迁

- 源码：`b40e6e2a925e76831934ed5db60170b4764151b0`。
- CI：https://github.com/laull9/RyuPy/actions/runs/41402483694 ，Linux x64任务1分45秒，success。
- 环境及固定版本同前独立桩探针，SDK10.0.403/Rust1.99.1/CPython3.12.13。

本次`publish.command.json`真实执行公开 `ryupy build ... --aot`，不是脚本自造宿主。
source/output路径实际含中文、空格、&。CLI报告本轮独占根下的绝对native路径；
`driver-publish.stdout.log`实有Generating native code，SDK stderr为空。两SDK日志在删除
源码、IL及完整driver工作树前复制，不能仅凭CLI“已生成”字符串称成功。

`result.json`及所有`logs/`逐字取自本次工件。all/argv/exit/error四模式分别通过固定CP、
JIT、搬迁native、无dotnet/Python容器，退出码0/0/7/1；JIT/native/container的两流逐字
一致，CP异常仅做已公开的路径/源码展示规范化。容器只挂native目录，禁网/只读。
实际二进制SHA256：`1ab6b89237e872f0177847f7648e722c7c708d8f35936c1ebd0e931758259c35`。
仍依赖Linux libc/libm/加载器，非全静态；未验其它平台、bridge、资源sidecar、完整语义矩阵
或性能。两fakeSDK失败测试与完整本地gate见../aot-driver-local，不混作真实ILC成功证据。

有限Linux x64 glibc CLI端到端发布现已闭合；整体NativeAOT/JIT优化仍继续。旧M3.11源码
不可用的事实不变。CI仅另报action Node20弃用提示，未降低应用trim/AOT警告门槛。
