# records显式实验安全门，默认不启用

固定当前dev 5d36db1e952f1db07aee6563d7a800cc1df94548，已读新AGENTS/全CIL方向；不改产品、ABI、provider或守卫。a1419到此提交仅文档变更，但本轮真实构建与完整check使用5d36。旧约11%原records新进程收益不是本轮验收结果。

两独立工作树分别默认和records-increment-experiment完整make check（fmt/clippy/tests/selftest/e2e），原日志、退出、真实TRX保留；不跳过新失败、不把feature局部测试当完整检查。随后继续原records默认/off/on准入结构，零命中停止后续；仅唯一命中保原CFG才运行源码门。

源码含旧五fixture与wide、capture。wide增加实体首key/等值key/删除重插/clear/小整数缓存、bool/float/None/overflow、混合域、256/257原槽/墓碑同Count变异与clear恢复、活迭代既有值更新/插入/clear失效、加法回调clear后返回/抛错与alias。诊断预期314fast/11fallback，所有事件真实执行才算验证。capture保留get先捕获、key求值后重绑get及hash抛错无半写；该复杂输入预计零rewrite，guard trace缺席或明确0/0（不将类型初始化等同实际调用），不为命中简化原语句。若新默认与CP有缺口则失败定位，不自动登记“已知”。旧known_callbacks/subclass也先由本轮默认JIT/AOT重新实证，再单列既有差异。

内存两层分列：产品同源码off/on NativeAOT，两尺寸1024/8192、held/cleared/released，各四轮AB BA BA AB独立进程；48进程每PID五次PSS/RSS(相关重复，非240独立样本)。输入握手后再验证字典key身份/值和checksum。released仅显式清根，无Python强制GC，不能称对象已释放。产品两臂各一次构建，不能称四轮独立构建。明确保存原MIR确认唯一守卫、实际SDK清单。

独立C# NativeAOT同一个Runtime二进制执行generic get/add/set或现成TryMaterialized（失败走原fallback），同48模式进程。每次保持n个存活或已clear字典，并创建四批n短命owner；NoInlining工厂仅返回WeakReference，完整GC后逐批断言owner全灭。记录逐批与最终GC.GetTotalMemory/heap/fragmented，采后KeepAlive保持根；弱引用数组自身及CWT容量驻留仍计入总内存，不能把live差额都称owner泄漏。没有任意百分比通过门，原始数值需独审决定；这是Runtime活堆，不是Python真实堆，不与PSS相互否定。

本轮没有重新跑四轮性能或默认晋升。复用source-trial附带的小fixture30冷样本仅附录。工件完整分片核验后才能评价安全范围，不因CI绿自动启用或推dev。

Runtime released同样先构建第5组owner，再由NoInlining工厂仅返回弱引用，完整GC断言收回；保留弱引用数组本身，不能把元数据成本当owner泄漏。
两层第五组创建次序不同：Runtime先建存活/释放组再四批短命，Python先四批再最后组；两层只能各自off/on对照，不逐值比较或把其GC历史当等价。

首轮43814354700默认完整check成功；feature的Runtime3349通过/3skip及Rust测试已跑，但cases-selftest因ryupy-e2e未声明裸records-increment-experiment feature失败，e2e编译执行阶段及后续准入/源码/内存均未运行。修订只把完整check的feature请求改为依赖限定ryupy-driver/records-increment-experiment，沿真实driver→MIR依赖启用，不改Makefile或e2e包；保存Cargo metadata及专门-p ryupy-e2e feature树，强制核driver/MIR节点和e2e→driver边，再原样完整make check。实验仍默认关闭，不因feature解析存在就声称运行期开启。


## 43816599556 捕获差异与下一次限定复测

本轮默认/feature 完整检查均通过；六个源码场景完成后，capture 的固定默认 JIT
先于实验臂失败：CPython 输出 old-get / 9，默认产品输出 new-get / 101。
原失败及原 oracle 保留，不能归因为 records 改写，也不能声称捕获语义兼容。
根因是普通 PyInstance 不属于提前捕获域，实参回调重绑 get 后再次查找目标。
通用预捕获会改变普通实例的绑定分配和 PIC 路径，单列后续语义/性能切片，不在此处修改产品。

下一次复测将 capture 明确标成 cp_equivalent=false，精确保留 CP 原输出，并要求固定
默认 JIT/AOT、实验 off/on 和诊断 JIT/AOT 全部重现同一个既有输出；任何偏离仍失败。
原复杂源码不改、两态 MIR 均须零 records 改写，诊断仅允许不存在或严格 0/0。
这是定位后单列既有差异，不是修复或扩大兼容性声明。其余六场景的合同保持不变。
内存门此前被跳过；后续只有真实完成后才能报告 PSS 和 Runtime 活堆，无阈值绿灯
仍不代表没有内存退化，必须独立比较各尺寸/阶段的四轮数据。


## 43822657192 内存宿主编译失败

默认/feature 完整检查及七个源码场景的限定对照已执行成功；capture 仍明确为
CP 不等价，原 records 的独立 JIT/AOT 诊断各记录 240000 fast、0 fallback。
随后独立 NativeAOT 内存宿主因三处引用内部 PyInt.Box 而在编译期 CS0122 失败，
产品 PSS 与 Runtime 活堆的 48 行测量均未执行，不能称内存验收通过。

后续仅把测试宿主的三处调用改用已存在的公开 PyOps.BoxInt(long)；该接口直接
转调同一个 PyInt.Box，保留缓存身份，不新增 friend access、不改变 Runtime/ABI。
原失败与证据保留，重跑仍需完成全部阶段并人工比较两层内存数据。
