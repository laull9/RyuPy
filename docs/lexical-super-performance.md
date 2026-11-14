# 实例词法零参super：有界性能观察

日期：2026-11-06。完整功能验收已过，性能风险未关闭。

## 工具与口径

A为7203d777的S0工具；B为未提交S1六参编译器1757a3f1…030f4db，最新Release runtime f7e50d90…086da8d。原固定生成PE/Host逐字复用，仅B更换runtime。完整新门禁2409 C#、900 Rust、518 CP、352 e2e（170 ignored）。

新runtime定点CIL及5份B smoke通过，真实PE生命周期24项另行通过；随后仅一次14进程/144暖样本，default A后B、tieroff B后A，每进程三样本。完整原流与CSV保留，无重试或补采。每样本按完成操作数与原items分母归一。

A/B是S0与最新完整S1候选，不单独量化此次旧super委托修正。只观察单配置一对进程，不推断显著性、长期稳态或总体无回退。

## 全部配对观察

| 配置 | 路径 | wall变化 | A/B线程分配 bytes/item |
|---|---|---:|---:|
| default | static_calls | -49.843% | 33.000000 / 33.000000 |
| default | dynamic_defaults | -7.717% | 33.000000 / 33.000000 |
| default | plain_def_create | -34.849% | 192.000000 / 192.000000 |
| default | closure_create | +6.971% | 256.000000 / 256.000000 |
| default | module_external | -9.630% | 24.625000 / 24.625000 |
| default | module_bare | -16.213% | 96.625000 / 96.625000 |
| default | resource_files | -39.365% | 1312.000000 / 1312.000000 |
| default | method_calls | -37.400% | 1.000000 / 1.000000 |
| default | generator_create_resume | -27.348% | 408.000000 / 408.000000 |
| default | class_module_body_execute | -4.915% | 1984.000000 / 1984.000000 |
| default | ordinary_instance_zero_value_call | +6.071% | 32.002930 / 32.002930 |
| tieroff | static_calls | -5.889% | 33.000000 / 33.000000 |
| tieroff | dynamic_defaults | +6.437% | 33.000000 / 33.000000 |
| tieroff | plain_def_create | +0.224% | 192.000000 / 192.000000 |
| tieroff | closure_create | -6.932% | 256.000000 / 256.000000 |
| tieroff | module_external | +5.960% | 24.625000 / 24.625000 |
| tieroff | module_bare | +10.136% | 96.625000 / 96.625000 |
| tieroff | resource_files | -2.840% | 1312.000000 / 1312.000000 |
| tieroff | method_calls | +2.453% | 1.000000 / 1.000000 |
| tieroff | generator_create_resume | +7.785% | 408.000000 / 408.000000 |
| tieroff | class_module_body_execute | +10.579% | 1984.000000 / 1984.000000 |
| tieroff | ordinary_instance_zero_value_call | -7.941% | 32.002930 / 32.002930 |

正值为不利观察，9条全部保留，不称噪声。22组分配中位数相同不等于时间相同或历史回退已解决。

## 新功能绝对成本

| 配置 | 完整操作 | wall ns/操作 | 线程分配 bytes/操作 |
|---|---|---:|---:|
| default | super_construct_consume | 800.069 | 128.002930 |
| default | empty_shadow_class_execute | 3814.779 | 1360.023438 |
| tieroff | super_construct_consume | 163.344 | 128.002930 |
| tieroff | empty_shadow_class_execute | 1612.337 | 1360.023438 |

super_construct_consume含真实代理构造、基类方法调用与tuple结果消费；empty_shadow_class_execute为真实类体/装饰器在空cell期间调用普通shadow。不是纯分配或单一ReferenceEquals成本。

## 结构证据与未证范围

新BuiltinDispatch的super分支直接调用双参LegacyConstruct，先关键词、再位置参数、最后MakeSuper。只移除super专用Func与WithKeywords；方法入口仍分配其他lambda所需displayclass。六参helper普通分支仍为原CallValue0，canonical才读frame。只审19个选中runtime方法；不是全DLL、全部ABI/metadata/反射布局等价，也没有机器码/Tier状态证明。

普通实例零参调用仍新增20B CIL与frame参数/身份分支，不推断JIT消除。S0默认method_calls +310.450%、generator +40.126%、resource_files +31.608%及更早不利全部未关闭；本次改善不能抵消历史风险。完整冷边界、144原始样本与进程证据随恢复检查点保存。未测完整项目/AOT/跨平台。
