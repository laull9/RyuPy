# 固定七配置NativeAOT裁剪语义验收

源码a0bd9227f146294b566f64e179b25b49d9aa1c20，2026-11-02，
[真实CI41405464604](https://github.com/laull9/RyuPy/actions/runs/41405464604) success。
同轮前置CLI无运行时容器smoke及Runner超时子进程回归亦通过。

5份原完整源码：BigInt、静态dataclass、动态dataclass协议、numeric-list岛、SLP。
前4默认，SLP默认/显式packed/显式scalar，共7次真实CLI发布；每源固定CPython3.12.13
一次，每配置JIT/native各一次，三流逐字比较全通过。没有更改原期望或默认SLP开关。
所有7份SDK日志均实际生成native；源码/native哈希、字节数及构建观测见summary.json。

整组60.74秒，各发布8.46–8.61秒；native约4.57–4.65MB。这是该runner缓存条件下
一次构建观测，**不是运行性能收益或可重复构建承诺**。host身份含本次唯一值，二进制
哈希用于本次工件校验，不用于声称跨构建bit-reproducible。

成功后清理各自host/bin、obj缓存，剩余工件91,970,516字节，保项目/IL/native/SDK日志；
不把约百MB每配置的构建缓存复制到Git。原命令、双流、原result及二进制仅Actions工件，
Git保存此摘要、固定配置与原result哈希。摘要不是用重序列化冒原始日志。

限制：只Linux x64 glibc；三SLP策略通过不等于AOT机器码已证明packed命中，亦不等于
真实ISA-off或NaN payload位级覆盖。二者仍明确not-tested；后续独立证据收口。
本组没有性能采样、没有关闭裁剪警告/全程序集root，不宣布整个N1或M5.2完成。
