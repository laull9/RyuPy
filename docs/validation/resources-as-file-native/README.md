# as_file 文件系统 NativeAOT 搬迁证据

最终候选运行时：as_file 入口为 internal，生成器上下文公开协议新增 SetAttr/DelAttr
两条覆盖。宿主 macOS 27.0.1 arm64，CPython 3.12.13，安装 .NET SDK 的真实机器。

[复现入口](../../../tests/aot/resources_as_file.py) 同次发布后复制完整 dist，
删除独占源码、JIT 和原 build/dist，换 cwd 直接运行原生二进制。结果见
[result.json](result.json)，保留 CP/JIT/搬迁 Native 的原双流和退出码。
三者均 exit 0、stderr 空、stdout 逐字一致；缺资源直接读取 exit 1，
stderr 为完整 FileNotFoundError traceback。

本目录保存最终第五次开发发布的原流。第一次成功路径通过、负例被 is_file 断言截断；
第二次修正直接读取负例通过；第三次用收窄 CLR 可见性并补齐错误接收者的代码重验通过；
第四次补齐绑定方法相等/hash/字典键协议后再次通过；第五次补齐混合错误绑定优先级后通过。
`publish_count: 1` 计该次独立 runner 调用，开发过程累计五次。

没有通过删除 PATH 冒称无 SDK 环境；该目标环境、Linux 与 Windows 仍待真实验收。
