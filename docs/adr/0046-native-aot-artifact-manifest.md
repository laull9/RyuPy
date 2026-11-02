# ADR0046：NativeAOT 发布清单与实际生成引用报告

状态：源码与本地专项／完整门禁通过；真实三平台 CI 尚待验收，未标记交付完成。
日期：2026-11-02。

## 目的与非目的

用户交付原生程序时需要知道本次发布到底含哪些文件、针对何种平台、由哪个真实 SDK
构建，以及源码模块和编译器所生成的静态引用如何关联。当前公开CLI可以真实发布并
搬迁 Linux x64、macOS arm64、Windows x64 原生程序，但只有文件路径与分散构建日志。
本切为成功的同一次发布提供版本化 JSON 清单和独立验证器，不改变 Python 语义。

清单不是签名、不是 ILC 最终根集或所有动态依赖闭包，也不是自动资源发现器。
首切不复制包数据、不修模块位置、不提供 importlib／任意动态 exec／原生扩展加载。
`dynamic_dependency_closure_complete`、`resources_included`、`native_retention_proven`
保持 false，验证器不得将元数据存在解释为这些能力已经成立。

## 清单来源与真实性

源码摘要来自本次实际传给 parser 的 UTF-8 文本，不重新读可能改变的磁盘文件，字段
应明确称 `compiled_text_sha256`。逻辑来源区分入口、普通模块与包 __init__，不泄露
宿主绝对路径、search_paths、用户名或环境。不同包同 basename 必须保持独立身份。
模块名、类名、函数名等程序元数据本身仍可能敏感，哈希也不提供源内容保密保证。

codegen 在同一次 Resolution 中报告真实 Main、模块 init/get/set 与实际 ldftn 位置和
目标；报告按显式生成映射分类，不从名称后缀猜角色，不额外保根、不添加IL指令。
未分类指针必须显式记录或失败，不能静默漏掉。字段称 emitted_delegate_references，
仅证明输入 CIL 中存在引用；不证明 ILC 最终保留、实际执行或所有反射路径可达。

平台、原生格式与体系来自实际 HostPlatform 校验。SDK版本由本次publish中的受控
MSBuild target写出 NETCoreSdkVersion；禁止用另外一次dotnet探测或工作流标签替代。
缺失、畸形、多行或意外预存的版本记录明确失败。compiler build revision若无可靠
构建注入则为null，不对用户工作目录运行git猜版本。

## 文件与失败合同

本次独占publish目录中的普通文件逐一记录逻辑相对路径、大小、SHA256与角色；不能
把未知sidecar猜成必需库。构建输入IL/runtime仅另记逻辑名称与摘要，不默认带入部署。
拒绝绝对/盘符/父级/非规范路径，拒绝symlink、Windows reparse/junction、设备等
非普通文件，拒绝manifest与本机文件名冲突。哈希采用流式读取并核大小/文件变化；
不宣称这提供针对恶意并发目录替换的无竞态沙箱。

manifest只在本次所有验证成功后通过独占临时文件写入并同目录提交，写失败不返回
成功产物。失败仍沿现发布ownedroot清理并保SDK双流；不宣称整个out包括旧IL写入
都因此变成事务。清单不列自己的hash，以免自引用；外部审计可另对最终JSON取hash。
签名、公钥分发与供应链证明属于后续能力。

## 独立验证与既有 CI

tests/aot验证器读取实际清单，对搬迁publish树所有普通文件做exact-set核对，逐项
检查规范相对路径、size与SHA；核唯一native、期望RID/格式/架构及实际SDK记录。
无清单、多文件漏项、额外文件、大小/hash改变、自hash、路径逃逸或链接均明确失败。
验证器对源码／生成引用只检查schema和身份一致性，不冒独立重建ILC可达性证明。

保原Linux七配置与双host四mode的fixture和stdout/退出/异常比较；不加性能采样，
不因清单通过跳过真实ILC或搬迁执行。macOS/Windows仍是SDK已安装的hosted runner
上删除本轮源码/IL后的精简环境搬迁证明，clean_machine_without_runtime_tested=false。
清单无绝对源路径也不代表既有二进制和原始SDK日志已经清理了所有源路径。

计划验收：纯codegen报告对实际PE、driver失败生命周期与SDK记录、独立文件验证器
正反例、同一次真实三平台CI原行为。此文状态须在这些证据齐套后再改，不以源码审
或伪native单元样本代替平台发布成功。

## 与下一包数据部署切的界限

只读最小复现已有明确结果：静态条件选择两个源码包模块，在移走原源码后JIT代码
分支继续正确；包内 `os.path.dirname(__file__) + data.txt` 在CP任意cwd成功，当前
JIT却因SourceModule.file仅basename而先落cwd读取失败。包目录cwd下成功只是巧合。
因此问题不只是AOT少copy数据，还需要把诊断显示名与真实／部署模块位置分离。
上述是本轮只读CP/JIT最小复现；后续资源切将固定永久fixture与CI证据。
当前没有针对该资源场景执行NativeAOT，不能把JIT失败说成已实跑AOT失败。
本清单切不修该资源缺口，清单CI原始证据另随对应CI artifact保存。

本manifest逻辑模块身份可作为将来显式资源清单的关联键，但不会自动建立运行时
__file__位置或复制data。未来显式原生依赖声明／生成C ABI绑定也是独立扩展；当前
无生产PInvoke/LibraryImport，C# native modules不等于C扩展，更不承诺任意wheel。

## 构建成本与依赖

仅AOT入口采集同次报告、源码文本摘要和发布文件摘要；普通emit/dump不遍历或构造报告。
loader增加逻辑来源字符串，不改变模块执行/诊断位置。driver使用serde/serde_json与
sha2的默认std实现（MIT OR Apache-2.0），不手写JSON或SHA，也不新增C#运行时依赖。
文件哈希/JSON/落盘同步增加构建时间与内存成本，未测量收益，不宣传零成本或产物瘦身。

## 首轮本地验证

3项真实PE报告测试、driver 26项（含3项来源测试）与8项Python独立验证器均通过；
完整门禁454项C#、228项固定CPython基线、202项e2e+30项ignored通过，无快照迁移。
首轮仅新测试的两个Constant名称因双glob歧义编译失败，限定为ryupy_mir::Constant后
原断言全部通过；生产语义未为测试放宽。SDK记录目标与真实原生清单仍待原三平台CI，
本地单元假文件只验证失败边界，不代替ILC及搬迁运行。

首轮三平台CI的Windows在发布前轻测试失败：测试以文本模式写显式CRLF，Windows
再次翻译成CRCRLF，严格SDK记录解析器因此拒绝。仅将测试输入改为精确UTF-8字节
写入，保原合法／非法断言与生产解析；本地8项重跑通过。该轮Windows未到构建／ILC，
不把测试修正算作实际Windows原生发布通过；Mac／Linux独立运行结果另行记录。
