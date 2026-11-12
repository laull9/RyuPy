# ADR 0161：普通多继承的冷 C3 与发布边界（实现候选）

## 一项权威

普通PyClass的MI族保存不可变直接基引用及C3序列；lookup、构造槽、属性/内建descriptor、类型关系、super和PIC消费同一序列。单继承Walk/IsSubclassOf保留原链快路；不复制父字典，不建立反向子类依赖，重绑沿用GlobalVersion失效。super缓存对MI必须再绑定真实接收类，不能只按词法owner缓存。

## 定义时序

只在同模块语法多基定义及其静态名字后代预登记真实动态公开槽；这不证明实际base身份。各基表达式仍按顺序只求值一次。新冷PrepareOrdinaryMiClass冻结引用且不计算重复基/C3；正文执行在未公开的真实类namespace，Finalize完成C3与全祖先/最终descriptor能力验证后填cell及内部类槽，最后应用decorator并写公开槽。失败不回滚已有正文副作用、不覆盖旧公开绑定。

没有把所有旧普通类移到新模型：旧ExprKind::Class读取静态槽缺少Unbound检查，简单移动其SetGlobal会把NameError变成null。因此新增一个冷ABI，保全部旧签名；旧MakeClass遇真实MI父类明确拒绝，不能绕开新发布合同。未知/跨模块单基MI、函数内词法MI尚不准入；语法多基的重绑普通值仍逐值验证，不按文本假设身份。

## 能力边界

仅Legacy JIT普通字典布局；真实object根、普通方法、property/staticmethod/classmethod、普通new/init已有算法的C3槽选择。MI实际类接收者的super只扩普通MI域，旧非MI类接收者拒绝保持。特殊内建/原生布局、CLR异常、Enum/dataclass/typing布局、自定义metaclass/descriptor/hook、结构__bases__/__mro__变更拒绝。旧非MI词法namespace规则不扩大。AOT仅对真正编译得到的Program内新MI冷调用报E0100，原soft/deferred来源先沿既有编译合同处理；Instance亦拒绝。类体traceback缺帧不在本单元修复。

## 验收

四个pending真实CP合同覆盖菱形、单基MI后代、descriptor/PIC重绑、失败副作用/旧绑定、new返回外对象及decorator观察。Rust检查真实发布槽与MIR顺序、CIL冷签名，C#检查实际类对象；本地HIR3/MIR1/CIL1、C#8及相关旧类合同共74项已执行通过；原CP四源中construction209B对齐，其余三源分别因旧instance.__dict__、class-body直接raise、Legacy globals边界保红。独立支持源保留全部MI核心机制，CP/JIT823B/499B/168B双流对齐；不证明原红源已修、项目+1或MI Native支持。固定20及默认AOT完整同源保护仍待；新两个实际driver图测试已核软解析失败保持deferred、真正MI产物AOT拒绝。类体helper引入额外frame，不称trace等价。
