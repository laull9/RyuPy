# 首组固定AOT/JIT配对：观测而非普适结论

源码bc351ac3a4937bb6b14d1b1584d09d3003671175，
[CI41407684705](https://github.com/laull9/RyuPy/actions/runs/41407684705)实际success；
同SHA的[CLI/七配置语义CI](https://github.com/laull9/RyuPy/actions/runs/41407684634)也通过。
固定5负载/三臂、45冷+36热=81进程全部完成，所有warm/sample checksum、stats schema、
退出码通过，未追加样本或删负载。SDK10.0.403/runtime10.0.14、CPython3.12.13，
AMD EPYC 9V74（runner可见4逻辑CPU）。完整方法见tests/aot/PERFORMANCE.md。

## 新进程墙钟中位数（ms）

| 负载 | 默认JIT | 禁tier JIT | AOT |
| --- | ---: | ---: | ---: |
| matmul64 | 118.36 | 191.38 | 17.86 |
| calls200k | 117.01 | 174.87 | 30.34 |
| dataclass50k | 111.89 | 184.48 | 19.15 |
| exceptions10k | 112.41 | 177.69 | 20.64 |
| hello | 79.34 | 132.35 | 10.27 |

cold仅新进程，OS文件缓存未清；wall包含启动/初始化/程序/输出/统计等，不直接等于JIT耗时。
此runner上AOT启动优势明显，但不是所有机器/项目的承诺。

## 固定预热kernel中位数（ms）

每臂3独立进程，每进程3warm+7sample，先求进程内中位再汇总；不称Tier1稳态。

| 负载 | 默认JIT | 禁tier JIT | AOT |
| --- | ---: | ---: | ---: |
| matmul64 | 5.051 | 5.225 | 4.972 |
| calls200k | 17.443 | 7.029 | 5.421 |
| dataclass50k | 4.753 | 3.350 | 2.599 |
| exceptions10k | 11.930 | 9.978 | 8.732 |

不能从默认JIT与禁tier的明显差距宣称固定预热已稳定；matmul差距很小，3独立点不足以
宣称普适加速。摘要保全部三个独立点及分配/GC/RSS，不只给有利的均值。

## 成本、不利项与下一观察方向

- calls整hot进程仍分配约147.5MB，AOT相对默认JIT只少约13KB；更快不等于去装箱。
- dataclass整hot进程AOT分配中位63,539,216B，高于默认JIT63,355,928B约0.29%，不隐去。
- exceptions整hot分配AOT约29.14MB、默认JIT约31.02MB；不把整进程数据称单kernel精确分配。
- 每AOT构建9.56–12.42秒，普通IL构建约3.55–3.81ms；发布包含restore/本机工具成本。
  首restore实有1.57秒，未隔离网络/缓存影响，不能称纯ILC速度或已全部预热依赖。
- 原生文件约4.54–4.59MB；publish含调试符号约12.42–12.50MB；JIT随附DLL/runtimeconfig/
  runtime约0.59–0.60MB但另需共享.NET。AOT另依赖系统库，不把这些数字当整机总占用。
- AOT运行期JIT指标汇总N/A，原stats仍保留，不以返回零计算“JIT减少100%”。

本轮未改任何运行时或优化默认值。下一步优先核calls动态算术/调用的实际机器码和分配来源，
选择单个有证据的有界去装箱/代码生成热点，不把旧PIC已复用数组误称每调用新数组。

Git只存此简洁结果及summary.json（含原summary/records哈希），不存raw日志/临时目录/
二进制；原始逐进程统计、IR与SDK双流在该Actions工件（当前保留14天，过期后仍有此摘要与固定源码可重新验证，不承诺原日志永久可取）。全部81成功后才清自有host/bin,obj，
不在计时中清缓存。首次本地IR预检lscpu环境失败历史保留在方法说明，正式CI一次通过。
