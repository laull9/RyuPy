# NumericList 一次三臂验收预案（尚未构建/执行）

基线 CLI 与上一 FloatLoop 候选逐字 hash 相同：3e599f3ecbb848b903289621e0f92c0743ef786f984e099af72b8e863b81b413。
基线 runtime：0ccd1c6695fa8e6981b1393c363072953307820e1e4ce3ea91c0b466769e3adb。
source.patch 复用上一已验证 FloatLoop 候选完整 patch，基点9023f21；capture-working-tree.patch 只是保存时工作树（已可能有 NumericList 编辑），不用于声称旧CLI来源。

## 三臂

A oldcompiled+oldruntime；B 同一oldcompiled+newruntime；C newcompiled+newruntime。三个独立目录/进程；B与A的程序DLL hash必须相同。计时harness各自复制匹配runtime，不能让加载目录偷偷选错版本。
正式一次，两tier，各3进程×7样本，三次同进程预热；进程顺序ABC/BCA/CAB。保全部原始输出/错误/退出码/墙钟；首语义失败立即报告而非改期望。

## 场景

1. 原matmul.py原规模100×100连续三次：make_matrix初始化在计时外，前一乘法结果直接成为下一输入，真正测生产者→消费者；输入创建成本不藏为零，另记录原main但不混称kernel。校验原固定13checksum。不得变为纯C#构造numeric输入。
2. numeric_list.py普通引用construct、append、read，重复同对象且逐项身份校验。输入/回调计数复位计时外；普通object列表仍应8B槽。
3. 同文件fresh append生产/消费float及bool/custom/mixed算术慢路。custom.__mul__次数严格校验。这里不能仅凭标签称NumericList存储守卫命中/失败：候选MIR/CIL必须证实。bool/custom输入是普通object storage；结果仍可能为float，仅代表计算慢路，不冒称16B存储降级。
全部乘法非累加叶来自GetItem：输入factors及消费result_factors在计时外建立，值全1.0；不使用乘法常量绕开契约。两个loop均fresh factory+append sink；第二loop写output后仅最终output[0]观察。正式时间前强检NumericCreate/ReadF64/AppendF64。
4. 如生产规则不匹配该微形状，保原对照并明确未命中，不为测量改pass；实际matmul是必须命中项。

## 独立资源与JIT

资源另进程3样本，读取allocated bytes/GC/JIT counters排计时表外；JIT独立default与禁tier，各臂实际matmul与普通ref方法，保完整asm/代码大小。核无每元素临时object[]、无不必要Box，核普通ref路径新增分支及容量/slot成本。16B槽不能用减少盒数掩盖小列表内存回退。
复用已归档core-harness结构，后续源码齐套再构建。验证不能强转内部List<object>，以公共Items/迭代协议读取，校验行为本身在计时外；资源snapshot在校验前。
不跑全26、不追加救测；至多父明确批准一次有具体根因修正。当前仅保存与脚本准备，没有固定13或任何benchmark执行。

微负载两个loop显式采用total/consumed独立槽，满足全函数观察约束；仅作为候选正例，不据此估算普通项目命中率。原matmul源始终不变。
