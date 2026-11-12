# PyText.Length 真实 AOT 对照

此门只测锁定 pyfiglet 1.0.2 原 CLI，不更新旧 Nuitka benchmark。

固定 before 为 `f1765bb40964ce6b251f40d291f0da95bcf44fde`，同一 Linux hosted runner、SDK 10.0.403、CPython 3.12.13 和 Release CLI，分别编译 before/after 的 Release runtime，公开项目构建两份 Native。每份先通过独立源码/PE/metadata/553 资源 receipt；源码、CLI、runtime、工具和产物身份全部冻结。

工作负载为 `-f standard hi` 与 `-f standard -w 20000` 加单参数 `hi` 重复 100 次。固定 CPython 原入口生成完整黄金；隔离项目源码后，每臂每负载先 4 次预热，再 40 次交替新进程样本。全部 exit/stdout/stderr 必须逐字匹配，否则不输出成功结果。报告原样本、分位数和 before/after 中位比值。

测量包含进程启动及本次字体装载/渲染，是暖文件缓存下的新进程，不是稳态吞吐、冷磁盘、干净机证明或全库速度。无 CPU profiler 或新的 Nuitka 测量；不拿旧 runner 的数字拼比值，也不把失败的普通长段落输入当性能样本。
