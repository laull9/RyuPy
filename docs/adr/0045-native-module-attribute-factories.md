# ADR0045：原生模块属性工厂的有限延迟构造实验

状态：固定配对完整但未达事前采用门槛，不采用；生产实现已回退基线，实验历史保留。
日期：2026-11-02。基线：`972fac072c2dbdd42e250f633a9e21b3d09cbb62`。

## 事实与范围

当前PyNativeModules登记28项，PySystem另登记sys，共29项；模块对象已在Import才建立，
但注册时提前构造属性表、函数签名及部分类型。目标是降低未用属性表的启动成本，
不是省掉不存在的Python模块执行，也不按用户源码import裁剪动态可达API。

首候选仅延迟13个fresh-table工厂：math、time、json、re、functools、contextlib、
logging、argparse、copy、datetime、string、warnings、dataclasses。其余16项（含sys）
保持原时点：io、unicodedata、textwrap、collections、collections.abc、itertools、typing、
__future__、random、enum、atexit、unittest、os、os.path、pathlib及sys。

io先登记的类型可由标准流/builtin open在import前观察；os.environ环境快照与
collections.abc/os.path主动导入及父属性身份不挪动。12个静态Lazy的同步、成功对象与
异常缓存合同不修改，random取熵时点不改。typing/unicodedata提前Value是基线事实，
不是本候选要延迟的内容。

13项逐跨入口审计：json/re异常使用既有PyType单例；functools静态wrapper helper不等于
partial导出表；warnings隐式入口不依赖catch_warnings类型；dataclasses.MISSING保同一
static对象。datetime/logging等类型只由其已导入Python API产生。不能据这些结论承诺
任意C#宿主手工new对象在import前的类型登记等价，也不在TypeOf热路径加自动导入。

## 唯一初始化合同

新增纯运行时RegisterNativeFactory能力，不新增生成IL ABI。保留原RegisterNative及
普通模块行为。新工厂每次Import尝试拥有独立状态/闭包：先缓存module并进入State1，
再调用factory；完整成功后绑定其返回的同一Dictionary，getter/setter不复制或重建它。
成功后beforeSet先执行，拒绝不修改表；重复Import与重绑定均见同一模块/表。

同线程循环Import返回同一State1对象，metadata可读、未建立属性正常报缺失。
**仅新factory模块未完成初始化时的SetAttr明确NotImplementedError**，不发明私有
返回字典与外部写的合并优先级，不把这项限制扩到旧原生模块或普通Python模块。
13个实际工厂均无Python回调/循环写；新API测试仍锁这一边界。

工厂失败移除本次模块缓存、保成功依赖、不挂父属性，原异常传播；下一Import新建尝试。
循环泄漏的旧失败module不能随新尝试切换到新表。外部已发生副作用/CLR类型初始化失败
不自动回滚，不承诺所有OOM/静态初始化失败可恢复。现静态Lazy不在重试宣传范围。

Registry/Cache当前无通用import线程锁：只维持已支持的单线程及同线程循环边界，
不凭一个factory/Lazy宣称全局线程安全，也不跨用户代码添加锁引入死锁。

## 成本与采用门槛

注册名字/委托仍存在，实际lazy导入会增加尝试状态与闭包成本；静态factory引用仍使
AOT实现可达，不能推断native必然变小。将成本移到首次Import不等于成本消失。
固定测量合同见[LAZY_MODULES.md](../../tests/aot/LAZY_MODULES.md)：120个冷新进程，
16应用构建加2CLI构建，同一1200秒硬预算，不追加样本或失败后刷到有利。

先语义/身份/副作用门禁，再按事前门槛评估hello分配与JIT/AOT墙钟；保所有导入负载
及负结果。未达标不默认采用，不扩大13项范围追收益。没有新公开调参开关、onefile、
资源收集或平台性能外推；Mac/Windows无SDK干净目标机继续独立待补。

## 实际决策

候选58e5958的120进程与全部构建完成，CI41429665933通过不等于收益达标。
hello分配J/T/A仅下降17.57%/17.48%/17.11%，均未20%；AOT墙钟仅下降2.19%，
未5%。JIT默认墙钟下降16.90%不能替代其它门槛；rebind-A墙钟增长3.43%与native
文件小幅增长一并保留。不追加样本、不扩到其余16项、不事后改阈值。

按既定规则将两处注册/Import改动与新增attempt/helper专项正常回退至972fac0，
不新增公开flag或留默认成本；实验提交、测量工具和两独立公开语义回归保留。
固定120工作流转为手动入口，不把完成实验当持续自动性能门禁。

完整数值、五点与限制见[精简负结果](../validation/aot-lazy-startup/README.md)。
