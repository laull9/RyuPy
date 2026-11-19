# ADR 0160：默认 AOT 的封闭来源计划（有界默认入口已验）

## 范围

普通 dynamic-module pilot 的默认 `build --aot`、非固定绑定且没有显式 Instance/include/resources/distribution-metadata 控制时，复用同一次默认来源固定点。真实 `-I` 根仍参与解析；不搜索宿主 site-packages、不解释执行 .py。旧显式 Instance 无资源清单仍拒绝，旧显式资源 schema2 和 binding schema3 不改变。没有候选或元数据需求时不生成自动位置，旧单源码/trace路径保持。

catalog Token证明真实来源，不证明所有语法。先沿旧JIT完整Instance资格纯查询选取，再对真正已选者验证 AOT AST/类能力。未获资格者保持原Legacy计划；已选者证明/编译失败不得降级或吞作soft失败。

## 双索引与发布

发现表 source 索引保存所有真实输入，包括 Deferred，供资源快照 open/hash；实际成功 unit 才取得连续 emitted ID，清单 sources/modules、入口和输出位置按同一映射投影。Deferred仅保来源hash、虚拟位置和原因，不造 ModuleDef/静态根。先前软失败不会错位后续模块。所有输入必须保持到ILC前stage验证。

自动位置沿 `_internal/resources/v1/` 与原portable/冲突/预算合同，位置不超过256。只生成虚拟锚，未部署Python源码，也不保证 `open(__file__)` 或数据存在。没有数据claims；真实资源缺失不得伪空成功。一般动态文件IO和任意元数据枚举不在完整性承诺内。

## 冷计划与对象权威

candidate payload v3覆盖全部 compiled/deferred位置，registration三分类为active（硬图已冷注册）、candidate（仅候选代码）、deferred（仅失败来源）。全部kind2，真实Native解析；普通dotnet执行仍拒绝。旧v1/v2保kind1。

`external_dependencies` 精确等原依赖目标减位置/outside/closed/protected，是未取得本计划源码权限的终端，不表示文件不存在，不授 ModuleNotFound、finder或导入权限。死分支未解析依赖不会在启动时被误作缺失；真正import仍走原查找。

所有字段/真实Registration身份/父包几何及预算完整检查后才发布计划。权限按Registration引用保存，真实模块创建时弱绑定；模块副本由捕获的代码对象取得原绑定，不从当前同名Registry或可写name/spec/file反推。新域父子任一侧具有权限而另一侧不匹配则拒绝，不能跳过父 `__path__`、finder或缓存规则。双方均不在新域时旧kind1和旧显式kind2行为保持。注册不是正文执行，初始化失败不回滚合法副作用或成功依赖。

## 清单及验收

artifact schema4：原资源形状加 default_source_plan（版本1、automatic_locations、原payloadSHA、自动Instance名及独立Deferred来源），sources/locations仅compiled；resources为空。scope旧四项false，declared_resources_complete=false，automatic_module_locations_complete=true。后者不表示数据/动态依赖完整。显式options不被自动选择改写。

须以真实默认Markdown77B/HTML102B、通用包正负例、Native原位/完整搬移/构建源不可达、实际PE静态根，以及原20项目/双全检/S1/ABI/显式资源类AOT保护验收。类体traceback帧缺失等已知边界不修不掩盖。此文不是新Native或全面默认AOT已通过声明。

## 2026-11-12 有界验收

[CI 43154611168](https://github.com/laull9/RyuPy/actions/runs/43154611168) 已完成本合同的限定默认入口与原保护核验；只覆盖已测源码/资源边界，不授任意动态资源、全反射、trim 或性能承诺。旧显式无清单拒绝保留，类体 traceback 帧缺失仍独立登记。后续普通多继承工作不由本结论自动获得 AOT 准入。
