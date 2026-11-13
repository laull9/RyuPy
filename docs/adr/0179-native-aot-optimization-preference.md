# ADR 0179：显式 NativeAOT 体积/速度偏好

## 决策

AOT 产品入口增加 `default`、`size`、`speed` 三个明确偏好，不新增 Python 运行模式。默认省略 SDK 属性，保持既有行为；显式选项映射到 .NET NativeAOT 的 `OptimizationPreference`，不是任意 MSBuild 参数透传。

配置读取层负责严格解析及来源边界，Session 携带本次请求，发布编排层生成固定 SDK 属性；不会把 SDK 编译策略混入 Python HIR/MIR，也不改语言语义。普通及固定绑定 AOT 共享发布处理。项目配置与松散 CLI 请求不隐式合并，非 AOT 不接受此偏好。

SDK 实际有效值写入本轮自有构建树，由发布器核实后生成 `aot-optimization.json`。显式选择若未生效则失败，不返回旧产物冒充成功。该记录不进入运行所需 `.dist`，不改变 schema4 运行清单，也不是增量缓存身份。

## 为什么不先做产物复用缓存

现有 owned-output 身份保护证明的是输出目录可安全更新，不证明 SDK、ILCompiler、链接器、继承环境和包缓存等全部构建输入相同。只凭源码哈希或旧产物存在跳过 NativeAOT 会引入不可靠的成功路径。本轮测量首次/重复完整构建，但不宣称提供增量构建。

## 取舍与验收

官方 .NET 10 ILCompiler 目标将 `Size` 映射至 `--Os`，`Speed` 映射至 `--Ot`，后者还可能改变 SDK 的 PGO 默认。不能把它解释为对所有应用必然变快。保留 warnings-as-errors、封闭依赖检查和目录事务；改变的是构建偏好，不是兼容性承诺。

真实验证使用另一应用链 binaryornot/chardet 文件分类 consumer，比较同机首次/重复构建、交付字节和两个实际进程负载，同时保留源码隔离、完整输出与干净容器回归。没有 NuGet/文件系统冷缓存重置；首次构建不等于全机器冷构建。测量结果待 CI 原件核实后补充，不将脚本或局部单测当作原生发布成功。
