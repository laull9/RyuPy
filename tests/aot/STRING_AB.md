# 字符串局部累加器：同runner A/B

本门仅测试设施，不修改产品。合入待验优化提交后运行，candidate为该CI的精确提交；
base固定为已发布归因快照`8eeae125feaff2772ed175b56e0557566de16069`，
其源码树与本地优化父提交`5655f497`一致。不得把未合入优化的设施分支称为优化候选。

## 构建和执行隔离

两份checkout、Cargo target、Runtime和AOT发布根互不共享，固定CPython3.12.13、SDK10.0.403、
Rust工具链。构建时显式指定各自`RYUPY_RUNTIME_DIR`，防止旧CLI读取新Runtime。
五负载均使用candidate checkout中的同一绝对源码路径；记录输入SHA、两提交、每个构建选项和原始进程命令。
所有ILC先结束，再运行语义用例、预运行、wall和诊断；JIT直接执行同次AOT的managed input。
本门不比较构建耗时为收益：baseline先构建，缓存次序可能影响构建时间。

固定五臂：直接CPython、base JIT、candidate JIT、base AOT、candidate AOT。
五负载：局部/模块`+=`与`join`，以及数值holder控制组。
字符串输出完整200000字符，每次双流与同次直接CPython逐字节比较，不再只比较长度/两端。
计时包含完整约200KB输出的格式化、管道传输及采集I/O，不能解释为纯字符串计算吞吐。
各臂输出一致仍不消除I/O占比对较快实现的遮蔽；结果仅代表此端到端新进程负载。
两项现有语义用例（数值与字符串累加器）先经CP/JIT/AOT双版本差分，失败即停止。
这只是固定输入正确性，不证明所有Python语义。

## 固定采样计划

每负载每臂一次新进程预运行（25），十轮计时（250）；五臂及负载正反循环，每一位置出现两次。
不自适应追加样本、不重试失败、不剔除慢样本。每次新进程、不清OS缓存，非暖态。
采样期禁用统计和实验/CLR profiler/加载器注入环境。全部wall结束后才运行25个统计进程；
诊断时间不纳入wall汇总。原始双流、命令、环境选择、超时、进程组状态保存在仓库外CI工件。
中断保留partial文件但不产生成功结论。分配/GC仅来自各独立诊断进程；CP分配不可比。

`wait4` RSS可能受fork继承高水位和测量下限影响，仅诊断，不作内存优势断言。
共享runner、有限微基准与样本、固定构建顺序仍有噪声；局部收益不等于模块级改进，
也不代表真实项目、Nuitka、跨平台或暖态吞吐。旧跨CI结果不能作为本门A/B样本。

## 运行

专用workflow `.github/workflows/aot-string-ab.yml` 在独立CI分支触发。
本地可先运行 `python -B tests/aot/test_string_ab.py`；真实构建需Cargo/.NET。
