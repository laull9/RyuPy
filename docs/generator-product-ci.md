# 生成器消费融合独立产品验证

候选源 `6bc254c9ea24a7138d5d320dc0618033612549ff`，默认关闭
`generator-sum-experiment`。独立CI分支，不随产品合入默认workflow。
测试实际提交以工件candidate.txt及Actions head为准；当前文档不是通过报告。

## 两臂与证据

同一源码分别冻结off/on两个CLI，两组独占Cargo target。两臂均包含新增Runtime ABI，
仅feature-on的编译器发射批量能力。因此A/B归因于能力发射，不是旧dev对整套ABI改动。
固定Rust1.99.1、CPython3.12.13绝对路径、隔离SDK10.0.403以及DOTNET覆盖路径。
默认make check保留完整Runtime TRX/详细日志；继承17实例与3个PE合同，另强制8个
编译ABI/实际桥合同全部通过。仅允许既有两项精确身份skip，不能称全部专项验证。
默认/feature分别跑定向25/28与完整MIR；完整只允许既有四项已知ignore。

普通pending源码显式CP、off/on JIT与NativeAOT四臂完整退出码/stdout/stderr差分。
AOT两臂保存真实清单并核SDK、批量委托角色：off为0，on须非零；这是实际发射证据，
不是每次运行命中计数。另执行中文空格搬迁，隐藏原发布目录、禁用PATH；不等于删除
全部源码或另一台机器验证。known空sum身份单列，明确CP=False而off/on=True；新增或
消失的差异必须失败并重新分类，不把它算作CP语义通过，也不加入计时集合。

同runner顺序测原始26项及生成器三消费者，1次丢弃预热、5次新进程样本；统计与搬迁
另轮。每臂复制真实SDK清单，样本/统计写盘且搬迁通过后才删除该项独占临时树；
复用list线已审的opt-in空间门/清理，保留生成器套件发现，不覆盖其他分支功能。
已知差异目录从未被性能发现。原26项包含list_ops/matmul；完整报告核26/3行与两臂
清单和空间记录。仅新进程wall/RSS，包含启动成本，GitHub共享硬件噪声仍在，不能
称暖态或全平台性能。Runtime桥接测试也不代替C ABI/NumPy专项。

## 有界运行与失败

job350分钟；检查60、MIR35、冻结35、语义35、性能160，共325，依赖安装15。
实际超时按失败处理，不承诺所有工作必在预算内完成。每次AOT构建前至少4GiB，
只删除当前job已冻结的Cargo target与当前用例临时树，不删除用户项目或别的任务。
工件总是尝试上传；总job超时仍可能阻止上传。缺根complete.txt不得宣称完整通过。
失败保留先前完成报告/清单、完整普通语义双流和控制台日志，不伪造后续阶段结果。

本地仅门禁轻测、Python输入/发现自检与静态审查；无dotnet/cargo，必须真实CI后
才能报告C#/Rust/JIT/AOT结果。CI提交独审前不发布。
