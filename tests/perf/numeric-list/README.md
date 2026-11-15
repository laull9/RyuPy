# 数值列表源码/AOT实验

仅用于隔离分支，不改变当前组合或 dev 的验收范围。固定旧 dev
81758bb477729aee4636f2ca6d6b0815b949849a 与含 Clear/Reverse 候选的冻结CLI，
内嵌各自Runtime；CPython 3.12.13，真实发布清单必须记录 SDK 10.0.403。

语义源使用既有双端数值列表producer形状；专用Driver测试保存真实MIR并要求
NumericListCreate/AppendF64/ReadCheckedF64。该门证明静态发射，不提供动态命中计数。
Python公开观察覆盖身份、活迭代器、NaN、负零和异构转普通表示；
C#五合同才直接检查内部背板与Items别名，两者不可混为一谈。
原始退出码、stdout/stderr均保留，对CP逐字节比对；两臂JIT与AOT均运行。
AOT复制到中文空格目录并移除本次原构建树，在无工具PATH下重跑。
构建所用独立输入也删除；仓库中同字节原fixture仍保留但不为该编译入口。
这不代表跨机器或无所有系统依赖。

性能有reverse、clear及list_ops控制三项，四轮分别AB/BA/BA/AB新AOT构建。
每轮每臂12个新进程，六种CP/A/B顺序各出现两次。普通采样无stats注入，
另一次instrumented进程核分配；GNU time保存RSS，计时含启动及测量外壳开销。
单独构建、源、CLI、可执行文件与manifest哈希留档，CLI/输入终态再核验。
四轮仅每臂四个构建，来自共享runner且并非独立机器；不能将微小变化解释为显著，
不能声称暖态、全面无回退或因果归因。clear保留16字节槽容量，少装箱不代表
总驻留内存改善。clear循环的float(i)本身已装箱，主要测试背板保留复用，
不能当持续去装箱负载。候选为本次精确workflow HEAD，不冒称仅产品ecc...原提交。无动态命中计数，不由MIR静态存在推导每次操作实际命中。

原始工件不入Git。当前本机无cargo/dotnet，只有Python语法、CP语义及差异空白检查；
全部产品编译、MIR与AOT结果以隔离CI的真实工件为准。


## Reverse-only复测
前轮9ebe9d9d/run43742146585中Clear复用负载四轮均变慢（4.3–7.6%），
不能归因于噪声。撤销Clear实现改动，保留Reverse，五合同中Clear改为
核基线物化/公开背板/再填充语义。完全复用前轮三源码及四轮协议，
新增完整make check及定向五合同。新结果出来前不能认为回退已经消失。
