# 目标 RID 的 Mach-O slice 收敛门

状态：真实产品验收中。生产实现只消费原原生重定位的拥有集合，不遍历任意资源树。

`run.py --python <固定3.12.13>` 构建两种独立的真实 universal 消费者：传统单阶段
`MH_DYLIB` 扩展、PEP489/heap-type 的 `MH_BUNDLE` 扩展，分别覆盖 fat32 与 fat64。
源输入含 arm64 与 x86_64，携带实际签名 identifier、runtime flags 和 entitlements。
应用函数由CIL执行，扩展执行真实调用、原生异常与身份回调。JIT、NativeAOT、OneFile
分别删除输入、wheel、虚拟环境与构建目录后搬迁执行，逐项比较当次 CPython 三流与终态计数。

验收包括：源WHEEL仍记universal2、转换报告有原fat表与选中slice、最终库存大小与签名后文件
一致、导出初始化入口保持、部署头只有精确arm64、codesign实际验签与entitlements一致。
AOT与OneFile另带显式fat原始资源，位于重定位拥有集合之外，业务实际读取其完整magic/长度。

`--expect-unconverged --mode jit --case legacy --cli <旧CLI>` 是旧入口红证，要求原部署
仍为fat且无收敛报告。结构门在 `cpython_macho_slice_tests.rs`，覆盖交换字节序、范围、
alignment、重叠、inner header冲突、缺目标沿原ABI诊断、subtype不明保原、非loadable保原、
权限与临时文件清理。报告的preserved只指不选slice，后续原重定位与签名仍执行；源安装文件不变。
无扩展名helper沿原fat32入口，fat64消费者沿原.so入口；不扩大原collector的magic准入。
普通文件不计算摘要，所有生成产物和原始日志写仓外并清理。
