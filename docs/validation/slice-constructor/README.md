# slice 公开类型验证记录

基线 HEAD `6db679ca3a81117c6ae08917988804d4b149f762`。固定 CPython3.12.13、
.NET SDK10.0.403、Rust1.99.1，Linux x86_64。范围见[ADR0099](../../adr/0099-slice-public-type.md)。

- reference.py/results.txt/stderr.txt 为独立 CP 参考原源/原流，包含未支持域，不算 RyuPy 通过项。
- 初次运行时专项14/14通过；公开JIT前独立审源发现四条漏口，已修并追加回归。
- 三公开CP-first应用包含两个新合同与上一轮未改写bytearray构造器失败源；先校CP原流、
  退出码及完整异常链，再允许编译/JIT。冻结比较fixture全部用户帧，不过滤未知stderr行。
- tests/fixtures/slice_constructor/pending 保复杂富比较/descriptor合法CP参考，不进入通过分母。
- 原bytearray fixture SHA256 `59ec8e4a65e2b8caabe8b11910a09d6b13f524cf95a69c7c8cfdfc85bb132a19`。

增补守卫后运行时21/21通过。首次公开批次1/3，但误用旧Release runtime，整轮不计当前候选验收；
构造源另有既有delattr名称拒绝，首源保pending后只迁显式del属性。重建最新Release并保存DLL哈希
后重新执行三应用及结构。完整原流、初次失败和重验分别归档，不改写历史。


有效首轮公开2/3，比较/完整六用户帧与原pending通过；构造在别名继承遇前置基类守卫NI，
已补PyRuntimeBases.Validate的slice精确拒绝及回归，再仅重验失败应用与MIR结构。

定向C#1/1、构造CP-first/JIT1/1及MIR结构1/1随后通过。三unique公开应用各自通过，
不把有效首轮2/3改为3/3。

首次正常完整make check exit0：fmt、严格clippy、workspace通过，1792项C#零失败跳过、固定CP基线228、e2e202及原30 ignored通过。新三应用与结构在门禁中全部通过。日志SHA256 `ddf3714906126f0968454472fc871d704e3c7d87ae34b44bf6090a9266d9d113`。
生产/测试文件与日志哈希见summary.json；完整原流随恢复包归档，DLL/PDB仅留哈希。

后续ADR0100已逐字执行原construct_indices_delattr.py.pending并通过CP-first/JIT及完整门禁；原源和上阶段失败历史仍保留，详见../delattr/README.md。
