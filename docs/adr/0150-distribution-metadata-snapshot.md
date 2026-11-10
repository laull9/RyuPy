# ADR-0150：显式封闭发行版快照（实施候选，未验收）

选择新增 --distribution-metadata 请求，而非伪造动态sys.path。请求schema1为
`{"schema_version":1,"mode":"closed_snapshot","roots":[{"id":"app","path":"root"}]}`。
根相对请求文件，顺序有意义；只发现根直接子项的dist-info/egg-info，目录内完整普通文件递归采集，不沿RECORD找文件。根内按路径排序。Name/Version身份与CP3.12路径优化discovery_key独立；同key首胜，所有被遮蔽原数据仍留在快照。

可信非并发输入是前提；拒绝链接、特殊文件、越根、未知schema与上限。前后两次采集检测一般变化，不宣称恶意竞态原子快照。未知元数据原字节保留。zip/customfinder不支持，明确拒绝。未配置entry_points明确NI，显式空roots才是空环境。

LinkedProgram只有一个可选静态payload；CIL机械发常量与配置调用，不读源目录、不改Main EH。
同runtime加载域只有一个生成程序集owner token与payload。相同owner+payload幂等，冲突/晚配拒绝且不部分覆盖；首次metadata import锁定状态。无选项旧产品不获得新的多程序集隔离承诺。

metadata使用普通Register初始化生命周期，首次真实import才捕获父import_module；每次初始化使用本模块独立动态字典。load动态读取所属metadata模块的import_module，目标导入/属性查找延迟，失败复用普通缓存移除链。

本轮API：import_module精确字符串绝对/相对名与普通参数绑定；EntryPoints迭代/len/bool/select(name/group)/字符串name索引；EntryPoint只读name/value/group/load。自定义/字符串子类导入参数、构造/值比较/hash、外围属性、extras加载明确NI；其他未实现metadata API不公开，保持普通缺属性/from-import失败。绝对name绝不触碰package对象。未知组合法返回空；未知筛选键明确NI。

不提前导入targets，不静默漏条目，不改Markdown原源/48与77B oracle，不增加默认20分子。完整Markdown扩展、默认find_spec来源边界与AOT实例资源是独立后继。候选必须经过差分/JIT/AOT搬移及旧门禁才可验收。

静态载荷预算：输入文件总计1MiB/2048项、256发行版/64根，JSON至多4MiB。
一个ldstr最多8MiB UTF-16；注册新增方法指令固定20字节，不把文本变为IL指令。
后端对所有ldstr不去重保守累计#US预算，超过24bit即拒绝；分块不能降低heap总量，故不另建拼接协议。
JIT/AOT显式构建均写distribution-metadata.json审计，同次AOT options也含report；运行authority仍为程序集载荷。

源码importlib父包不能意外取得native metadata子模块：由内部不可写native注册来源位判定，混合域明确NI。
源码同时提供metadata时仍按既有源码注册覆盖规则优先，不借公开__file__/__name__猜来源。

修订002：只公开已实现entry_points、EntryPoint、EntryPoints及load使用的import_module绑定。
version/files/requires/Distribution等8个未实现高级名字不公开，普通属性访问为AttributeError，from-import沿原机制失败；不发布NI函数去遮住ImportError fallback。
无选项时metadata模块现在可以导入，但entry_points调用明确NI；这是新增有限模块的导入时机变化，不是默认完整metadata支持，也不能将成功import作为兼容证据。

分发读取仅支持Linux/macOS的x86_64与aarch64。逐祖先lstat拒链接，末端用该OS/架构NOFOLLOW|NONBLOCK打开，随后核句柄普通文件/尺寸及dev、ino、len、mtime一致；有界读取后再核句柄与路径。其余平台拒绝该选项。仍以可信非并发输入为前提，不防恶意父目录交换，也不声称原子快照。


修订003：native metadata的EntryPoint/EntryPoints替换与删除在本轮域外，构造NI另行登记。
向这两个名字赋同一真实类型对象允许；不同对象在字典写入前按ReferenceEquals拒绝，不执行用户比较。
import_module与entry_points仍允许普通重绑；源码metadata自己的setter不受此native限制。
