# ADR-0159：LRU 两个精确字符串的局部查询探针

状态：实现 WIP；CP 唯一合同窗口已通过（exit0、stdout19B、stderr0），C#、新JIT/Native与分配对照仍未运行。CP事实不等于候选产品或性能验收。

## 选择
非零容量、非typed、无names/values且两个精确CLR string时，用readonly值探针保存两引用和一次tuple哈希。命中不物化PyLruKey/List/数组/PyTuple；miss才创建原键表示。只共享原tuple hash步进内核，长字符串原hash弱表可能分配，不承诺任意输入零分配。没有按项目名特化、全局缓存、新公开ABI或RuntimeFn；单key/kwargs/typed/PyStringInstance等仍原路。

## 语义不变条件
stored-left比较可运行用户代码，不能用string序比较替换。完整候选比较后才检查原版本，变化重试；调用数组可能被内部回调修改，探针不重读它。miss持久键用快照和预算hash，但被缓存函数仍接原args。锁/计数/真实LRU/递归插入不覆盖与函数锁外执行不变。

真实相同hash对象二项键、truthiness/NotImplemented/throw、clear/递归/内部args改写、barrier并发及强引用释放新增专门回归；原LRU测试完整保留。C#数组变更是内部因果例，不称普通Python可直接取得Invoke数组。新Python例仍pending；冻结CP输入已按一次预算执行通过，不补采黄金，尚不代表RyuPy通过。

## 交付门
先编译全部Runtime与原+新LRU测试，公开ABI逐签全保，再原固定项目与同PE wcwidth暖JIT/Native搬移。分配对照使用同Host/同次数/预构造输入、区间外检查身份和hits；JIT和Native分开记录。分配少不等于更快，未测速度不作收益承诺。任何回调顺序差异均阻断，不豁免旧红例。
