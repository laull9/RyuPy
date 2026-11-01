# NumericList 唯一修正后的最终三臂验收

2026-11-01。首轮完整负值见相邻numeric-list-first，不覆盖。这里只做父批准的一次具体修正复验：双typed背板（普通wrapper+8B）、ToIndex长整型快路/cold异常协议拆分、小intrinsic inline。多个因素同时改变，不能把收益归某一个；无第三轮救测。

## 三臂与真实来源

- A：与首轮完全相同的旧FloatLoop DLL+旧runtime 0ccd1c66…。
- B（即B2）：与A逐字相同DLL+修正runtime 0347f3878919bae370decd00533d6f322126285baf3f4bee5cd32ef04400f0a5。
- C（即C2）：与首轮NumericList候选逐字相同DLL+同B修正runtime。

本轮没有重编CLI/MIR或程序DLL。numeric_list/matmul MIR、CIL与首轮逐字相同，binary-hashes.json记录每个程序体及其实际harness/runtime。只替换B/C artifact/harness中runtime；harness本身复用，API未变。
source.patch基于9023f21，包含当前tracked+untracked crates/runtime完整源码。程序体编译来源为首轮patch，当前patch额外包含runtime唯一修正，不能说当前源码重新编译过所有程序。

基线/tmp/ryupy-m3_10-numeric-list-baseline-4y3zve，首轮/tmp/ryupy-m3_10-numeric-list-candidate-tWv1iY，最终/tmp/ryupy-m3_10-numeric-list-revised-p1ypjG。所有二进制只留/tmp，不归档DLL/CLI/bin/obj。

## 完整口径

预设一次两tier、3进程×7样本、3预热；每进程组ABC/BCA/CAB轮转。原matmul100²连续3次不改规模，输入重建与checksum校验外；真实compiled方法反射调用，非C#替身。micro30000轮输入建立/引用身份/自定义回调计数验证外，两个生产消费loop全GetItem叶+独立累加槽+fresh append sink，是候选命中正例而非通用命中率。

三臂完整语义、独立资源12进程、时间36进程、JIT12进程全部exit0。资源与计时/asm分开；没有全26或额外统计重复。固定预热不保证所有默认PGO完全稳态，保后台JIT计数和全部版本。time表不把原main冷启动与kernel混合，wall_seconds包括进程启动/初始化/预热/验证。

## 最终时间 ms（每进程7点中位数后跨3进程取中位数）

|场景|default A/B2/C2|disabled A/B2/C2|
|---|---|---|
|matmul|106.016 /94.828 /73.657|147.448 /150.502 /86.575|
|ref construct|.791 /.701 /.559|.530 /.535 /.460|
|ref append|4.691 /4.320 /4.717|1.949 /1.787 /1.788|
|ref read|2.277 /1.639 /2.022|1.339 /1.858 /1.157|
|float|6.905 /4.469 /1.445|2.294 /1.747 /1.570|
|bool|8.966 /11.035 /13.188|5.536 /5.037 /6.589|
|custom|25.533 /25.900 /26.133|11.468 /11.131 /10.712|
|mixed|5.703 /5.350 /5.728|7.147 /6.668 /5.920|

matmul C/A default-30.5%、disabled-41.3%；C/B-22.3%/-42.5%。float C/A-79.1%/-31.6%。不能由此称全路径净赢：
- bool首守卫失败C/A+47.1%/+19.0%，C/B+19.5%/+30.8%；失败守卫成本是真实风险。
- 默认custom C/A+2.35%，mixed+0.43%，ref append+0.57%。
- 禁tier普通read B/A+38.83%，C/A却-13.60%。见下文机器码相同证据，不能强归编译算法，也不删除不利值。
- 两轮A本身有不同时间（matmul default123.916→106.016），故只使用每轮内三臂比较，不拿跨轮裸时间相除当修正收益。

## 分配：普通+8B/list已实际计入

两tier一致：
- append_refs A524648→B/C524656B，+8（1个PyList）。
- construct_refs A480288→B/C480304B，+16（2个PyList）。
- matmul A1391448→B1393872B，+2424=303×8；C1278672B，对A净少112776B/8.10%，对B少115200B/8.26%。
- float微负载1428224→1428240→1428256B；小产物数值槽反而多常数分配。
- bool/custom/mixed B/A+16、C/A+80B；read三臂1427776B相同。

存储每普通元素仍8B object槽，但wrapper加第二引用是实际空间回退，不能用“普通列表未增加成本”描述。微负载下标盒仍在，非零分配循环。

## 实际JIT与范围

禁tiermatmul A/B均2879B且全文SHA924d6d69de825beac1c502b21da158b7952af250310ce87be4d47eb6d8322465。
C为4563B（首C3059B）。CanReadF64/PeekList/ReadF64/AppendF64入口全inline，热边没有这几项call，cold ReadError仍call。micro produce_consume A/B4859B、C7450B。无FMA；大幅代码体积增加须作为默认预算代价。

默认保全部Tier0/OSR/Tier1；不能因Tier0仍有helper call就断言优化后从未inline，也不挑最小版本。
本轮独立asm同时捕获runtime入口：
- 禁tier普通construct_refs/append_refs/read_refs三臂各自全文相同，206/295/1107B。
- PyOps.GetItem三臂全文相同1004B。
- B/C PyList.GetItem(object)全文相同238B；A为71B，另有ToIndex185B、GetItem(long)59B调用。

因此普通read的B/C差异不能归不同read算法或callee实现；整体布局、进程阶段等影响未唯一分离，原样保留，不再救测。源码与机器码证明数值helper成功inline，不能据此推导任何规模下都更快。

## 复现与终态

使用首轮相同源码/三个产物目录，B/C复制修正runtime（包括harness bin目录），先完整--diagnostic/原matmul校验。运行run_three_arms.py ROOT resources/time/jit，各输出目录必须不存在。summarize.py保每进程与逐点数据；jit-summary.json按方法拆分hash/字节，binary-hashes.json明确装载runtime。
本轮JIT过滤额外包括GetItem/ReadSlot/AppendValue/MaterializeAll/ToIndex与numeric helpers；只影响独立asm进程，不污染正式时间。

18:14:30 UTC全部实际JIT终态，18:14:36前已通知父窗口释放。以后只文本归档，不构建/运行第三轮。默认取舍由父/审阅者综合失败守卫与代码体积决定；报告不宣称普通路径零回退。
