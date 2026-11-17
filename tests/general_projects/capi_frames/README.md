# 通用 C API 调用现场验收

两个独立原生扩展读取公开 C API，而不是由被测程序补造 globals。
`native_scope_probe` 读取实际 globals/locals/code/frame；`native_callback_probe` 使用
PEP 489 状态保存逃逸 frame/code/thunk，并经真实 traverse/clear/dealloc 清理。

- `run.py --python <固定Python>`：直接复用产品 header 的机械门；检查真实 D/D、源文件、
  定义点 builtins、原生异常、逃逸 thunk 失效、活动 thunk 重复拒绝、构造失败回滚。
  来源登记检查 code 复制不继承身份、手调活 weakref callback 不撤销记录、逃逸 callback
  无失效裸指针。大常量表仅作复杂度诊断，耗时与正式性能分开。
- `runtime/RyuPy.CPython.Tests/StockThinProgramLifecycle/run.py` 的 `call-carrier`：
  实际 provider DLL 与正式 shim 的 CIL 入口门；JIT/NativeAOT 分开执行。
  Python A → 普通 managed 协议保持 A 的真实现场，新增可信 CIL B 则由独立产品验收。
  `--audit` 显式固定 3.12.13，验证相同文件/函数名仍有独立 code origin。
- `observe_function_frame.py --python <固定Python> [--aot]`：普通 Instance 函数完整 frame
  的真实 CP/CIL 差分，创建原生 wheel，构建后删除源码、wheel、虚拟环境并搬迁运行。
  `pending` 输入保留原断言：globals 已对齐，完整函数 locals、名称和逐语句行号仍待实现。
  `--case uncaught|system_exit|native_error` 分别验证普通业务异常、退出消息、原生错误
  的原退出码/错误末行，以及真实载体/来源登记归零。所有原始三流写仓外日志。

普通 Host 关闭的强别名与用户析构门位于 `OrdinaryCloseCases.cs.in`。活别名仍能读取原
frame；丢弃别名后由关闭 GC 回收不可达 CLR owner，再排空合法 CP decref。
用户析构实际重入 provider、报告原 ValueError，并在 atexit 中核对事件顺序。

完整函数 frame 输入的语义差异持续显式保留；关闭资源是否归零独立校验。
所有二进制、wheel、构建输出与原始报告都在仓库外临时目录，结束后物理删除。
