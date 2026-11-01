# 四core独立同进程预热方案（未构建、未运行）

## 不混口径

原26suite wall=每个新进程Popen至wait4，main_ms含该进程入口后的JIT；--warmup只丢弃其它
进程，不能称本进程充分预热。alloc/GC/JIT为全进程累计，maxrss为wait4峰值。输出最后一份
stats的原report之外，runner另存每个子进程raw stats/stdout/stderr，含warmup用于审计。

四core另列“真实编译函数kernel同进程预热”，不是原main稳态。加载真实程序集并执行入口
初始化（其冷成本不计入kernel），绑定实际m0_*方法，预建参数/输入；同进程至少3个完整kernel
warmup，再7个kernel样本，3新进程、B/C轮转、默认/禁tier分表。长kernel的3完整warmup不能和
之前短field_reads的20次等同，需采一次实际JIT版本验证并标明仍可能后台分层影响。

## 四个kernel

- fib：真实fib_rec(30)一次+fib_iter(90)200000次。使用CreateDelegate绑定实际(long)->long
  方法避免每次反射参数盒；结果832040和2880067194370816120。这多出C#delegate调用边界，
  不冒称原CIL directcall成本；如JIT去虚化须用asm确认。
- mandelbrot：实际mandel(double,double,long)，完整240×120坐标、max1000。绑定typed delegate，
  数值网格循环保原坐标公式；输出网格/校验在计时外。刻意排除Python字符串拼接/print，与原
  main单列，不能用该kernel时间计算原main加速。
- matmul：真实make_matrix(100,1/2)在每个样本计时外重建a,b，计时内连续三次真实matmul(c,b,100)，
  和原基准相同计算。结果checksum用相同遍历顺序与固定13 round(...,3)校验；不把第一次输入
  构造纳入kernel。动态签名用预建object[]反射（每kernel仅3次）或确证的object delegate，明确
  边界开销；返回列表仍真实分配，GC按kernel测前后差值另轮采集，不能清空结果伪零分配。
- nbody：真实make_bodies+offset_momentum在计时外重置，每kernel调用真实advance(bodies,0.01,100000)
  一次。初末energy通过实际energy函数及固定13输出校验；不沿上个样本继续推进状态。反射每kernel
  一次，返回None不当状态错误。

## 实现与验收

纯/tmp C# harness引用配套runtime，读取产物真实方法签名后绑定；typed/static与Dynamic签名若
实际不同必须诊断，不动态猜字段布局。CPython参考单独同源函数kernel runner，runpy输出重定向
仅用于载入函数/初始常量；预热/7样本同样排初始化与输入创建。所有函数结果逐样本验证。

计时进程不开disasm、不采alloc；独立resource模式采kernel前后alloc/GC pause/JIT delta，外部
wait4仍给进程峰值RSS。两者不同口径单列。不能把kernel分配差值与全main累计混算。默认与禁tier
各3进程，未知结果/签名即停止该项并报告，不切换到手写算法或降规模假通过。
