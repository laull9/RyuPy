# 有限 Decimal 独立 Native 门

复用主CI已有 `ordinary_mi` 选择信号，但使用独立 job、脚本、fixture、黄金及
artifact。CLI使用普通默认编译配置，不启用源码pilot；Decimal是原生模块。
不改旧 MI 原件、默认源计划或任何项目分母；提交标题仍为既有精确
`validation: ordinary-mi-c3-001`，没有新增路由开关。

图中实际构造有限 Decimal，核尾零、负零、超28位整数、str/repr/空 format、
copy 身份、int/bool/float 双向比较/哈希/字典两插入方向、十亿绝对指数和IEEE
最小 subnormal 的精确值。subnormal 字符串由固定 CP3.12.13 在准备原件时展开，
产品不调用未准入的 Decimal(float)，也不使用浮点最短显示作为精确值。

顺序为固定 CP 黄金自证、双 PIC JIT，再发布唯一 ELF，核实际managed/runtime
与清单摘要、执行前完整dist归档，隐藏本门源码副本，原位/搬迁分别核exit0及
精确stdout/空stderr。finally恢复源码并检查冻结身份；失败保原流，不重跑或
用检查器通过替代Native产品结果。Native发布600秒，job40分钟，工具10分钟、
验证25分钟；中间obj/bin不上传，必要SDK输入、真实dist、工具和原流均保留。

此门不是 Decimal 算术、Context、信号值或完整模块支持证明；本地无clang时
仅可报告CP/JIT/检查器结果，真实Native须等同提交hosted证据。
