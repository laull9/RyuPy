# 三条既有 Phase 3 基准正式转正

完整原规模四组与轮转测量均已 ready 后，移除 comprehensions / decorators /
generators 的旧 pending 头，再运行固定 CPython 3.12.13 的真实输出校验：

`python tests/run_benchmarks.py --runs 1 --warmup 0 --no-cpython --save m3_9-promoted comprehensions decorators generators`

三项均正式通过，脚本退出0。该单次记录只证明去标记后的入口可执行与输出一致，
不用于性能收益结论；正式配对/资源/JIT见相邻 full-suite 目录。
