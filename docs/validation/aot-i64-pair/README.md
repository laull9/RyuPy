# actual-I64短Add链：同机固定配对结果

状态：达到事前有限候选数值与代码体积阈值；**尚未推广main/default**。
用户交互输入正确性作为独立阻断处理，不以性能通过抵销；不继续添加样本挑收益。

[CI41411848974](https://github.com/laull9/RyuPy/actions/runs/41411848974)真实成功；
baseline `457886017cd4d32911a09b09307d07c327b87192`，candidate
`e614d6cb300640b3093f4d3b4d774b7e77545c3b`。两干净checkout分别重建CLI/runtime，
每版三后端使用同一次AOT的输入DLL；不是拿旧跨job基线冒因果。

预注册[合同](../../../tests/aot/I64_PAIR.md)完整执行：24次应用构建、180个进程
（90 cold、72原hot、18 guard）、1008个计时子样本和全部warm/checksum通过。
仅每组合3个独立进程，不称1008个独立样本；30分钟硬预算内实际139.36秒。
raw/stats/source/native/runtime哈希和summary已独立重算核对，见[精简数据](summary.json)。
原始工件在该CI artifact保留14天，可能到期，不是永久raw日志备份；Git不保存raw日志。

## 收益与机器码

calls的整个热进程分配三臂均约147.48→99.55MB，下降32.50%，仍包含初始化/输入/
3warm+7sample，不除以调用数伪造精确kernel分配。calls热kernel默认JIT/禁tier/AOT
中位分别改善22.39%/16.26%/13.40%。固定预热不证明默认JIT已Tier1稳态。

本轮实际native：positional1738→1963B（+12.95%），caller两者480B。
原整数成功段首Add后的中间RhpNewFast消失；候选两LEA之间无call/盒，每步xor/and/
负号分支仍回原慢路，末结果仍一次Box或小整数缓存。整个函数仍有F64/慢路分配，
不能按全函数分配call数量误判。六个native文件总字节大小均相同，不代表代码零增长；可能含布局/对齐影响，
不将具体原因未经验证地写成定论。

## 负结果与限制（不因门槛未触发而删除）

- bool guard默认JIT热中位慢5.54%，三点并非全部慢；custom guard默认JIT三点均慢，
  中位慢4.31%。两者未触预注册“>5%且三点皆慢”的组合条件，**不等于无回归**。
- float-J第二候选点7.453ms、对应基线5.112ms仍保原值，不称已经证明是噪声。
  overflow-J中位慢1.85%，失败/溢出路径不是零成本。
- cold单项：dataclass-J慢7.21%、dataclass-A慢17.25%、exceptions-A慢31.36%；
  hello-A快26.80%。cold几何比J/T/A为0.9990/0.9922/1.0254，未越1.05，
  但几何汇总不能抹掉这些单项。cold只是新进程，OS页缓存未清。
- 此前[首次配对](../aot-jit-baseline/README.md)的dataclass AOT相对JIT分配反多0.289%
  仍有效保留；本实验不改写该历史负结果。各负载本轮整进程alloc/GC/RSS三点亦保数据。
- 两版runtime源码未变但独立构建DLL哈希不同，分别核对实际使用副本，没有偷换同版。
  构建包含SDK restore/缓存等开销，不能视为纯编译器速度；Linux runner观察不泛化所有平台。

候选语义门禁：5 MIR、3真实driver、1真实PE和完整434 C#/226固定CP/200e2e+30ignored
通过，无快照更新。性能和语义证据支持有限候选资格，不宣称完整AOT优化或打包等同Nuitka。

相关标准输入CR阻塞随后已在独立main修复并通过真实macOS矩阵，见
[CR证据](../stdio-cr/README.md)。该修复不在本轮e614d6c性能产品中；本次没有因此
追加性能样本，也不将旧测量冒称对后续每个提交的重新测量。
