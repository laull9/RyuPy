# ADR0141：Legacy生成器动态异常目标

- 状态：候选实现；固定CP前置已验，hosted产品门禁待运行。
- 基线：本地fb0553f与CI等价快照0c7e39f同已验源码树。

## 选择

完整20固定JIT入口为10通过/8未支持/2运行失败。全诊断与四个锁源CP入口观察表明，
star/globals和模块发现不是一个小切：networkx实际还执行compile/exec，Markdown需要
真实独立loader执行，pyfiglet是资源包缺图。拒绝按单条首错承诺多个项目立即绿色。

click._termui_impl.raw_terminal的yield/finally/except termios.error，以及networkx
single_source_all_shortest_paths的yield/except nx.NetworkXNoPath是两个真实同机制需求。
前者是当前原入口图编译阻碍；后者被star挡住且固定入口不调用，不计实际执行收益。
选择把已有普通动态异常CFG组合进已有生成器状态机，不扩运行时解释器或动态IL。

## 合同

仅Legacy的SourceDynamic生成器链新增准入。目标保现有Name、Name起点属性链与无展开
tuple；异常发生且轮到本项时才求值。错误替换、同级跳过、as清理、裸raise、finally、
throw/close及暂停状态复用build_dynamic_try与generator_exceptions；frame按原LocalId
保存ambient/selected/flow及所有临时。PyGenerator暂借caller异常链，yield后恢复caller。

InstanceNameDynamic及GeneratorFrame来源化合同继续拒绝；不改默认Instance集合或
Native自动Legacy、不支持except*及复杂目标AST、不开放termios或完整networkx。

## 证据计划与限制

六组完整公开源、独立普通observer、无observer错误帧与精确Instance负例分别验收。
固定CP首次observer/default/Instance负例退出0且stderr空；错误帧预期TypeError退出1。
CP只有程序/期望有效性，不是RyuPy结果。全部仍pending，先hosted编译验证fixture前置。

MIR单测必须按实际LocalId证明保存/恢复与第二handler索引，真实PE验证getter失败不进
同级、as清理在外finally前。当前static/noEH控制结构不变量与旧完整功能门分开；没有
acceptedfb实际相同控制输入的对照，不称历史结构、分配或时间相等。

hosted完整门禁、同轮20逐ID保护及progress恢复、新综合Legacy Native原位/搬移/隔离，
七旧Native门均保留。未执行结果不勾完成，不因障碍前移增加分子；无新性能承诺。
