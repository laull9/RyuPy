# ADR-0059：zipfile 尾记录识别与真实 BadZipFile

状态：本地完整验收通过，尚未发布。

## 动机与范围

2026-11-03 在未发布候选c0e0b43上，锁定pyfiglet1.0.2原固定入口各一次CP参考、CLI编译、
JIT执行：前两步成功，JIT首败 `ModuleNotFoundError: No module named 'zipfile'`。
原输入、官方sdist、源码及产物哈希已独立核验；这不是公开main或完整项目通过。
仍未到字体资源读取，不增加历史5/20项目计数。

本切提供通用 `zipfile.is_zipfile(filename)` 和真实 `BadZipFile`，不是空模块或按字体名
返回False。实际pyfiglet还在压缩字体/安装分支需要ZipFile、namelist、成员open/read等；
本切不提供这些API，不承诺固定入口下一次会成功。

## 识别合同

以固定CPython3.12.13的实现为基准：检查末尾22字节EOCD快路，或最后65557字节中的
最后一个EOCD签名，再处理可选ZIP64定位器/记录及前置数据。保留对应的字段位置/大小
检查，不增加普通EOCD中央目录、成员、CRC或注释完整性验证。is_zipfile的True仅表示
这套尾记录识别成功，EOCD-only等仍可能True而ZipFile打开失败，不能用.NET ZipArchive
打开成功替代此谓词，也不能只看文件头PK。

参数先按普通read属性探测决定流/路径；借入流逐次调用seek/tell/read，不擅自缓存方法、
恢复位置或关闭。内部按路径打开的流自行关闭，包括失败路径。只有CP对应范围的OSError
和BadZipFile映False；其它用户异常不广泛吞掉。已处理的回调异常恢复本API入口帧与context，
不改全局异常机制。BadZipFile使用真实CLR异常、canonical类型、工厂与静态导入/typedcatch
接线，不用Exception别名或仅类型名字字符串替代。

## 资源与限制

普通文件每次读取最多65557字节，ZIP64增加固定小记录读取，无解压/全文件物化。
自定义read可返回超过请求长度的数据，按实际返回bytes/bytearray只读扫描，不截断、
不伪造False；复杂度随实际返回大小增长，不承诺任意用户回调的恒定资源上限。
可变bytearray的span只在无用户回调的局部解析阶段使用，不跨下一次seek/read/tell调用。

路径输入沿现有open/PathLike范围（str、Path及返回str的__fspath__），不新增bytes路径或整数文件描述符支持。
ZipFile、解压、中央目录/CRC、加密与压缩算法、完整任意buffer/算术鸭子协议不在本切。
需要对未支持合法正例保持pending，错误值不能偷换为“不是ZIP”。本次不新增任意ZIP
解压预算、文件系统提取策略或跨线程保证。

## 验证状态

已独立源码审并完成首轮解析专项：14项C#测试、4项真实CPython/JIT差分全部通过。
真实BadZipFile另有4项C#、2项HIR及2项driver测试首轮通过，覆盖构造参数、派生、
静态来源/遮蔽、typedcatch和普通源程序行为。

新增PE测试首轮失败：测试把dotnetdll的parent.method显示形式写成了parent::method，
构造调用字符串断言不匹配。保留首轮失败，改为直接核实际MethodReference、父TypeRef与
完整构造签名；修复后PE专项1/1通过；其后正常完整门禁首轮通过：fmt、严格clippy、679项C#、
228项CPython基线、Rust工作区及202项端到端测试通过，保留30项既有ignored。
未设置快照自动接受或弱化原断言。

无AOT、性能或20项目矩阵；旧matmul禁分层回归保持开放。未重新运行pyfiglet，
不宣称越过该项目下一阻断或增加项目通过计数。
