# ADR-0097：bytes/bytearray ASCII分类族

日期2026-11-04。状态：有限合同及隔离源码审通过，运行时专项23/23、三个公共差分首次3/3通过，正常完整门禁通过。

## 动机与范围

阶段A可变regex之后，候选0509d90的锁定chardet单入口CP0/compile0/JIT1，实际无帧错误为bytes缺isalpha；没有据此推定唯一内部调用点。本切实现通用精确bytes和bytearray的isalpha、isalnum、isdigit、isspace、isascii、islower、isupper、istitle，不特判项目或模式，不称完整二进制方法库。

唯一固定CP3.12.13参考在30秒预算内0.032380秒exit0，stderr空，88行/7族。源码SHA256为7babd978c020b232e626b70a3d7c25d374da2963e01bb1f95e3cb8c2a8ec4526，stdout为c96dcd4efe55652a3fc88c27d25dc92788ddecf63a25c96d087140adb4dd41b1。两载体全256单字节、32混合串和四调用路线的原流独立核验；新应用的完整异常链/root直接错误组合仍须真实CP-first验证。

## 语义与绑定

alpha仅A–Z/a–z，alnum另加0–9，digit仅0–9，space仅09–0D及20；四者须非空且全字节满足。isascii允许空值，00–7F均真。lower/upper忽略uncased字节但要求至少一个相应大小写字母且无反方向字母。title中数字、标点、NUL及80–FF都是新词边界；每个字母段须首大写后小写，至少存在一个cased字母。不能使用Unicode字符分类或locale推断。

direct、保存bound、根方法及保存root使用同一扫描。bound先拒关键字再拒位置数；root先检查位置self存在及真实载体类型，再进入同一绑定。另一二进制载体、memoryview和可转换对象不是合法root self，不调用用户转换协议。家族窄分流不更改其它保存方法的既有参数规则，自造错误在抛出点Prepare，用户参数求值异常不捕获重包。

bytearray在每次实际调用时活读，保存方法持原对象而非内容；调用前的同长写、resize和view alias写可见。扫描没有Python回调，不持导出、不阻止之后resize。仍不承诺外部CLR线程同时修改公开存储的安全性。

## 实现和成本

按方法选择一次只读span扫描，O(n)时间、O(1)额外状态，无文本解码、内容副本、逐字节delegate或全局内容cache。正常零参不切片或复制args。bool装箱、现有调用包装仍有成本。共享CallMethod及PyBuiltinMethod.Invoke头部新增少量接收者类型判断，GetTypeAttr新增有限判断；不能宣称整个公共路径零开销或性能提升。

根对象仅有限可调用描述符和已有元数据，缓存只经限定2类型×8名称入口，至多16项，不永久intern用户字符串。完整descriptor.__get__、用户类借放、bytes/bytearray子类布局、memoryview分类及Unicode str分类不在本切范围。未新增编译器MIR/CIL/RuntimeFn，不改变regex租约。

## 验证

新增14运行时Fact，覆盖全值域/活读、根与保存方法、错误时点/帧/context、原参数副作用和普通对象不劫持；原准备方法表及相邻binary affix回归一并运行。三个CP-first/JIT应用覆盖值域/保存接收者、完整绑定与求值顺序、完整异常链，首次CP预期不符即停。随后正常完整门禁；各终态据真实日志补记。此时未新跑chardet，不代表项目通过、main、AOT或性能基准。

首次内部专项23/23通过（14新增及9既有prepared/binary affix回归），零失败跳过。三个真实CP-first/JIT应用首次3/3通过，分别44/32/5标签，原值域/错误输出及完整用户链相符；并非仅参考脚本通过。当时首次正常make check进行中，终态另记；未执行新项目入口。

首次正常make check随后exit0：fmt、严格clippy、workspace、1680项C#零失败跳过、228项CP基线、202项e2e和原30 ignored全部通过。日志SHA256为`0bf64a1705daafa33033295dbae064574642ecc4477963922102048f165fe2ca`。源码/三个应用原流/产物hash独立核验；归档保78项输入与原流等文件，DLL不打入源码恢复包，原目录产物另已核SHA。该门禁阶段候选仍未发布，尚未重跑chardet，不增加历史项目通过率或性能声明。

## 分类族之后的固定项目单入口

冻结候选2cfab57bc31b6af96292bca8828be8cf9d15148c随后在原预算内只执行一次锁定chardet 5.2.0原入口：CPython exit0（0.164945秒）、编译exit0（1.669454秒）、Linux JIT exit1（0.615978秒），总2.451498秒，无超时。CLI单次准备后冻结哈希；官方501源码/入口/golden均保持，30/90/30及总180秒限制不变，首错误停止无重试。

实际JIT stdout空，stderr为mutable/view regex subject未支持。完整现场链为entry4→__init__.py49→universaldetector.py274→charsetgroupprober.py70→hebrewprober.py240→charsetprober.py73；对应原源filter_high_byte_only中的re.sub调用，pattern和replacement均bytes。这个有帧证据不同于前轮无帧isalpha错误，仍不能称项目通过。CP仍是固定19字节golden及空stderr。

结果JSON SHA256 `a3884b556a4f4796b55207211a303a86677569cded61be78f6c47e67c25e95d5`，JIT stderr SHA256 `6ff7907c549cafdce857bf24285fa03dd1165f1a3ae02ebb9af67f317e06b1bc`。这是未发布候选的单入口Linux JIT记录，不更新历史5/20，也不外推完整chardet、main、AOT或性能。后续按已记录CP生命周期设计可变subject的sub/subn，不能只替该调用转bytes快照。
