# 生成器消费融合独立产品验证

候选源 `18fd69d5a0e23461dc788dd58dc80fcaf2f38b09`，默认关闭
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

job350分钟；检查60、MIR35、冻结45、原输入定位10、语义35、性能145，共330，依赖安装15。
实际超时按失败处理，不承诺所有工作必在预算内完成。每次AOT构建前至少4GiB，
只删除当前job已冻结的Cargo target与当前用例临时树，不删除用户项目或别的任务。
工件总是尝试上传；总job超时仍可能阻止上传。缺根complete.txt不得宣称完整通过。
失败保留先前完成报告/清单、完整普通语义双流和控制台日志，不伪造后续阶段结果。

本地仅门禁轻测、Python输入/发现自检与静态审查；无dotnet/cargo，必须真实CI后
才能报告C#/Rust/JIT/AOT结果。CI提交独审前不发布。


## 原始重绑定输入的历史基线定位

保留原失败输入字节，与已运行的远端4d831f4d中的旧pending源码即时git show逐字节核对。
准备阶段显式fetch精确旧失败提交并核FETCH_HEAD，不能假定全历史checkout包含不可达对象；
fetch失败直接停止，不跳过门。另构建固定dev81758bb4，不能用当前off臂替代。先采集CP/dev-JIT/off-JIT/on-JIT四路
完整退出码和双流；dev若成功而候选失败直接硬失败。仅当dev恰为登记的sum未绑定
NotImplementedError、退出1且stdout空，两候选逐字节完全一致，才登记为1个独立未支持
输入。这不计入普通语义通过。若dev或任何候选发生其他结果，失败并重新调查/分类。
随后仍须运行修订普通输入的CP/off/on JIT/AOT、批量委托真实发射、已知空sum身份、
搬迁及全部性能门。修订普通输入把sum/range重绑定置于显式绑定参数的函数内，不删除
原始障碍，也不称原始模块行为已支持。原输入固定dev定位未运行前不能提前归因为基线。

## 第二轮隔离验证补完（未运行）

本次集成源提交：跨模块输入46c3c1ef、工厂计数说明833c9f65、实际dev ABI门fd21afff；
保持原产品开关默认关闭，不在本CI中开启默认产品优化。实际组合树以运行HEAD记录。

两态各跑完整make check。feature-on使用依赖路径
`CARGO_FEATURES=ryupy-driver/generator-sum-experiment`，因为e2e包没有同名本地feature。
每态先保留cargo metadata真实resolve，核driver/MIR特性位和e2e依赖链，不能只凭命令
文本称启用了feature。两态都真实构建并执行Runtime测试，保留各自TRX/PE证据。
先从固定dev在外部构建目录生成真实Release Runtime DLL与不可复用manifest，
显式GeneratorAbiBaselineTest=true核旧全部签名+精确8增量（及负例）。这不替代古老
1659条基线的既有skip。检查PE证据确实存在、无删除、实际/期望全集相同，不只看测试名。

跨模块普通输入入口cross_module/main.py：off/on分别JIT/AOT对CP完整双流并搬迁。
AOT清单必须同时绑定main/producer/consumer三个源码模块身份、实际编译文本摘要和
链接模块名；off零批量role，on精确三个不同真实target。三个发射入口不是三次运行
快路命中；溢出输入要求正确回退，不把它当成功快路消费统计。

保留原26项及三个消费者1预热+5样本保护；再用原generators和三个消费者做4轮×12pair。
每轮重建两臂AOT，冻结CLI不重建；奇偶轮交换off/on的AOT构建先后、套件和用例顺序。
每轮测量执行六种CP/A/B排列各两次，使三臂位置和off/on先后均平衡，另外单次预热
不计入样本。默认配对脚本行为不变，平衡和逆序必须显式开关。
逐轮保留命令、构建顺序、执行顺序、raw samples、SDK清单、统计和搬迁；按实际
CLI身份规范化off/on。四轮48新进程样本只有4次独立构建，分配每轮另取一个值，
共享runner/启动/I/O/布局噪声仍存在，不宣称暖态、显著性或全平台性能。

第二轮步骤预算：依赖15+旧ABI15+两态完整70+MIR20+冻结30+原输入10+语义35+
原26/3保护70+平衡70=335分钟，job350分钟。任何未完成都按失败保留证据，
不能以较早轮次数据替代缺失部分。真实运行前必须独审；本地轻测不是产品通过。
