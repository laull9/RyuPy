# 2026-11-05：真实 dict 默认布局

## 基线与目标

基线 c69c4e9，先前完整门禁1942 C#、228 CP、202 e2e、30原ignored。ADR0105仅修精确更新基础，不曾开放dict派生；docopt最近原窗口仍是79a04f7的Dict(dict)基类E0100，没有JIT。本轮实现不按docopt类名分支。

## 已执行的语义核验

- 独立CP参考九源及本轮补充合同保留。六主应用分别验证记录型dict、业务init/repr/missing、默认消费者、活视图、scalar/NaN/比较及JSON/Counter/proxy/percent差异。
- 原主应用的baseline编译失败全部留存；字面量dict展开是既有HIR缺口，先拆pending。首次候选六主4过2败，显式两参super与view-set比较另拆pending；原源、golden和失败日志不覆盖。
- 保存root方法改为业务方法返回 `super().get`，保真实无参super上下文与公开self。主golden不变；这不是原含显式super源已通过。
- 候选003六主CP/JIT复验6/6；只将首行pending换空行后再次6/6，行号和其余字节不变，七合法pending文件未改。
- 第一轮C#1982过、4个旧dict拒绝断言失败。迁移为新增canonical dict准入与仍拒tuple的断言，不删覆盖。迁移后1986全过；审查修复新增18项后2004全过。
- 新4项HIR测试通过，验证dict别名/后代、公有发布槽与重定义、循环固定点、来源遮蔽与既有特殊类路径。

## 独立审查发现与处理

1. 初稿机械复制的list标志、TryLookup输出参数次序，在构建前修正。首次构建仍因FunctionMetadata.DeleteMetadata误复制不存在的value guard失败，保源码与日志；只删除该错误重复块，写元数据入口的NI守卫保留。
2. 初稿wrapper Item无条件扫全表，将O(1)变O(n)；改用wrapper scalar不变量，仅精确root冷域扫描。root popitem、空update和wrapper view查询亦避免无谓全表扫。
3. exact target接受wrapper raw源必须扫描目标键域，否则scalar源也能碰撞目标custom key回调。该新域明确NI且无hash/eq/写入副作用；旧exact/native raw成功域保持不变。
4. 原view_foreign源证明values吞foreign反射且结果错，keys/items的!=误调用eq。局部view dispatcher分别执行eq/ne，缺ne才eq回退，显式ne=NI不重放。表达式非bool结果仍明确NI，容器内部允许Truthy。
5. 原scalar_collision源证明共享PyHash bool叶分支把非数字送进NumericEqual导致TypeError。两端bool分支加IsNumber守卫，用户比较已先派发，四向bool/字符串与exact/wrapper存储测试锁住。
6. 新dict根循环比较不经Python函数递归守卫，静审发现潜在CLR栈溢出。没有运行不安全版本；新根加线程局部256层保护，CP确认RecursionError类别/文案，同身份自环与失败后恢复测试通过。阈值是明确保守边界。

最终独立五源CP/compile/JIT均exit0；其中四源stdout严格相同、stderr空。第五源仅custom-key目标更新NI差异保pending，不说整源支持。两个原真实失败源hash不改并已严格对齐；追加chr(1)双向碰撞消除字符串转义歧义。

## 实现取舍

PyDict/普通PyInstance字段保持原样。新wrapper独立attrs与不可替换payload；PyDictView仅增加两个bool，旧构造不变，实际B/op由性能测量而非静态推算。新布局scalar键禁止用户hash/eq进入存储操作；不是一般stored-hash merge/重入修复。

fromkeys、JSON、Counter、mappingproxy各按自己的公开/根协议选择；不统一实现IPyNativeMapping剥payload。正常业务覆盖保留，核心槽覆盖先冷NI。尾删裁剪避免重复popitem的二次复杂度，迭代同尺寸结构变更、复杂视图及copy/deepcopy等有清楚暂缓边界。

## 性能与门禁

同生成CIL/host、预绑定delegate、四ABBA进程和7样本已准备：32旧路径对照，28wrapper-only。候选00360项与旧32项smoke通过；中途host双程序集__main__初始化冲突只在外置host修复，原失败保留，正式两侧使用同一host。计时中不跑build/测试/项目。

实际性能、正常完整make check及干净提交后的唯一原docopt窗口在完成后补入，不提前计项目通过，不改变固定20项目分母。

### 性能完成记录

默认与固定JIT配置分别四ABBA，合计2576真实样本，均成功。所有32项可比B/op不变，exact视图32B；新wrapper视图公开调用64B。默认ordinary构造中位受明显阶段转换影响，保其+65%聚合不利结果与后段样本，不声称已测出最终稳定差。补充固定配置ordinary构造+7.1%、exact256 iter-first+26.2%、exact list append/pop+7.3%、普通属性+6.2%；默认iter-first未同向，配置之间不混算。完整全32项及各run/样本范围均归档，具体限制见ADR0106。

### 正常完整门禁完成

`make check` exit0，2004 C#、228 CP、202 e2e、30原ignored。逐名核对新增六主driver与旧更新六主均通过，HIR100含新增4结构测试；30ignored与前轮完全相同。最终runtime72590fd2…，没有push或AOT验收。接下来仅干净提交后的原docopt唯一窗口，不将能力扩展预报为项目成功。

### 原 docopt 固定窗口

功能提交c0786d19f1aa581d1b9c47d6e7c4b19e9674d5c6、干净tree70177b1216c33fb9bf3218bc97208c0eb7b07023上，只运行一次未改docopt0.6.2原入口。官方28文件/归档、入口、golden与工具均核hash不变；CP exit0，16字节stdout对齐，原四条SyntaxWarning共732字节及hash与旧窗口一致。compile exit0，原Dict(dict) E0100消失；首次JIT exit1，stdout空，首错docopt.py:466 printable_usage的 `AttributeError: module 're' has no attribute 'split'`（调用链入口10→docopt558→466）。总0.263秒，无超时/重试。

这只证明编译阻断解除，docopt仍不计通过。下一候选为通用re.split，不能按该项目特判。未另跑完整20项目矩阵、AOT或跨平台，不改历史5/20及不同候选的既有单入口记录。result.json SHA256：31c72bd90589931ae3fc17d88c995c304374926f7ee519fe1efdb36f13e033e3。
