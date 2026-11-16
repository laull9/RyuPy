# ADR0122：真实有限 find_spec 与实例执行边界

- 日期：2026-11-05
- 状态：已实现有限发现；完整门禁、八份公开源、真实CIL与唯一性能窗口完成；原docopt通过，Markdown实际首错推进到native/legacy发现域，实例执行仍未实现
- 开发基线：1f5daca9ee714fdbaf333b8559d402d159a3db30

## 选择

ADR0121 已建立真实 ModuleSpec 和源模块元数据，原 Markdown 仍缺 importlib.util。
完整消费需要 find_spec → module_from_spec → loader.exec_module → sys.modules 别名。
其中发现可独立实现，执行则必须先解决实例全局。选择先交付可单独验收的发现 API，
不注册空 util，不用 Import(target) 偷取规格，不复制静态模块字典假装独立执行。

实现仅在 runtime 查询真实登记表，保原 Rust 编译器和生成 PE 布局。未缓存目标每次新建
规格/loader；父初始化与缓存读取依 CP 顺序。部署域外无否定存在性证明，明确 NI，
不把“没有编译进来”改写为 CP 的 None。完整合同见[importlib.util](../importlib-util.md)。

## 身份与开放边界

loader 保真实 CompiledModuleLoader 类型，新增保证 bool 的字段相等和 name/path hash；
未开放独立执行。两个精确字符串字段的常用比较无临时字典；字段形状变化后所有当前字段
仍参与比较，值先比身份再比标量。不同复杂容器/用户值明确 NI，不在快照后执行回调。
ModuleSpec 对未替换的内建比较作窄豁免；用户替换的比较仍不吞非 bool 原结果。

旧缓存 ABI、普通静态全局、失败旧模块别名和模块删除限制不改。17份新完整程序区分
当前候选域与延期域，原 cache/nonpackage/clone/failure/parser 断言均完整保留。
可写缓存、hooks、路径变化、自定义name/package、实例执行和真实html.parser仍未完成。

## 阶段证据

18项公开入口 C# 先在没有 util 注册的实现上产生红灯，全部真实因缺少 importlib.util
失败，原流与trx保留。接线后定向43项中42通过；并发源编辑窗口使新增异常真值子例
与运行时编译输入可能不一致，保留1项失败，不把它删去或记作通过。随后冻结源、强制
重建runtime，只复核该fact通过，前后源SHA一致。完整门禁仍须使用最终冻结输入。

首批15份CP程序各双轮共30进程成功，原stdout一致、stderr空；另两份独立只读缓存/
非包父程序各单次成功，不复跑原完整程序。CP证据只证明程序/期望成立，不算编译通过。

后续验收记录将区分完整门禁、真实同PE runtime配对性能和原项目固定入口，不改旧
同提交20项目矩阵；未实际执行的阶段不写成完成。

独审发现父路径逐层守卫、hooks执行前相位、native父分类、loader NaN/重入、
外层异常context及异常对象属性协议六点问题。新增七个最小回归先实跑为六红一绿，
修复后最终冻结输入54项定向测试全过，构建前后十份源SHA一致；所有原红灯保留。
独立源码复核确认六点闭合；显式finder配置隔离执行验证另列，不用代码审查替代测试。

## 最终功能门禁与公开源

冻结源frozen-source-001上正常make check退出0，耗时142.066961608秒：2312 C#、
791 Rust workspace、450固定CP基线、450 cases selftest、318 compiled e2e全部通过，
136 ignored保留。冻结11386文件在门禁前后及接续复核均无变化，Release runtime
SHA256为5cbaa9deb3e524d90ada91fed58cb003840e7e88648679b2386ccc8dceaf15ba。
门禁时HEAD仍为1f5daca9ee714fdbaf333b8559d402d159a3db30，这是冻结候选验收，
不能说旧提交自带新功能。证据为check-001-summary/source-verification/before/after及原log。

八份完整源uncached_regular、parent_initializes_target、relative_names、binding_aliases、
argument_effects、parent_failure、cache_readonly_supported、nonpackage_parent_supported
均经实际编译/JIT，stdout逐字等CP3.12.13期望、stderr空。转正只改pending首行与case清单，
正文及行数不变；九份完整延期树未删改。另六个隔离进程验证finder配置/父路径的显式NI
与相位，仅是本实现边界验证，不计作CP差分通过。原红灯、并发编辑阶段失败与最终修复均保留。

## 真实CIL与唯一性能窗口

708共同类型完整CIL/元数据独审通过，旧公开ABI保留，普通PyFunction/PyOps/PyPic全部
方法不变；新API八个生成函数真实消费公开find_spec/比较协议。共同PE和Host同字节，
只换上一切ModuleSpec runtime与当前Release，未重编共同源或混入旧正式样本。
唯一default/禁tier矩阵12进程、264暖样本、12首次Main成功，无补采、异常值删除或挑最好值。
所有共同路径暖分配不变，但首次Main新增2008B；禁tier dynamic_defaults两配对
+41.34%/+17.77%，default resources.files−26.01%/+48.44%，其余不利全部保留。
不能从CIL相同或分配不增推出无回退，也不关闭上一切ModuleSpec与overload风险。
完整数据、公开函数绝对成本、冷启动边界和身份见[性能报告](../importlib-util-performance.md)。

## 干净功能提交的原项目窗口

功能提交1be506924d94978f5bfa862d981e29ee6965d21c，tree
7499b41015c0151405cedc33d97260881c118e90，工作树干净。最终文档提交只追加结果，
不改变该功能/测试内容。2026-11-05 23:31 UTC在上述固定compiler与当前Release runtime
上各执行一次原docopt0.6.2、Markdown3.5.2的CP/compile/JIT，限时30/90/30秒，未重试。

- docopt三阶段exit0，CP/JIT的16字节stdout逐字等原golden；JIT stderr空。CP原四条
  SyntaxWarning共860字节照实保留，不能描述为所有阶段stderr都空。
- Markdown CP/compile exit0，CP的77字节stdout等原golden；JIT exit1、stdout空。
  原traceback定位markdown/htmlparser.py第39行，实际调用find_spec('html.parser')，首错：
  `NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented`。
  这是已明确保留的发现域边界，不是module_from_spec/exec_module已运行。后续源码的实例创建、
  loader执行、sys.modules写入和parser差分断言在本窗口均未到达；不能改原入口绕过。

项目脚本整体exit1反映Markdown失败，不能改写为全通过。两份3187文件源树、63旧输入、
62矩阵仓库输入、原入口/golden、20个CP根顺序、每项目自己的编译搜索根、工具及HEAD/tree
前后均一致。源树聚合SHA256为93bbcd6bf508ab37acce038fb7b491e50c16472706d2f69dbc3d1cbbfaeb7955。
证据在私有恢复材料importlib-util-validation/projects-final-001的result、audit.before/after、
全部原stdout/stderr/command/result与输出产物。这里只验两项原Linux JIT入口，不改原20项目
同提交矩阵、不增加Markdown分子，不外推完整库、AOT或其它平台。

下一步先核html/html.parser的真实登记来源与发现合同，再闭合实例全局及执行链；
不能给现有native空壳伪造SourceFileLoader或来源路径，也不能仅复制模块属性假装独立实例。
