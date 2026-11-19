# ADR-0245：由原版 asyncio 驱动 stock 实例模块的 CIL 协程

- 日期：2026-11-19。
- 状态：限定 `native-coroutines` 编译 feature 和显式 stock 模块命名空间的产品首切。

## 决定

协程函数继续生成 CIL 工厂、驻留帧和恢复方法。向 CPython 输出时使用两个静态 shim
类型：协程暴露真实 `am_await` 槽和 `send`／`throw`／`close`；独立 await 迭代器
另有迭代槽，协程本身不可普通迭代。CPython 的结构化 Coroutine 协议能识别该对象，
不修改 asyncio，不运行替代业务正文，不要求动态 IL 或新的 Python 胶水。
从 CIL await 外部对象时，在 CPython 查真实类型的 `am_await`，保留返回迭代器，只取得一次
迭代器，不从实例字典取同名方法。通过同一委托转发完成值、异常和关闭；
`PyIter_Send` 区分暂停和完成，不能以普通 for 的布尔迭代接口代替。

任务、Future、事件循环、contextvars 和取消发生在实际的原版 CPython 中。CIL 恢复仍按
协程定义点取实例模块环境；挂起后不重新按模块名查找。候选关闭时保留原实例模块诊断。
这不是精确 `types.CoroutineType`／`cr_*`／`inspect.iscoroutinefunction` 等反射合同；
异步生成器、旧式生成器 awaitable、全部重入及未等待告警等边界尚未验收。

## 验收与边界

- `asyncio_tasks` 两个独立源模块由即时 CPython 对照；使用真实 `loop.create_task`、
  `asyncio.wait_for`、Future、`current_task()`、contextvars。验证工厂惰性、暂停终值身份、
  定义点写回、任务和 Future 的取消、`finally`、实例同名属性不是 await 槽。
- 固定 3.12.13 和 3.13.3，macOS arm64 的 JIT、真实目录 NativeAOT 和 OneFile 六个删除输入
  搬迁产品通过；来源均为 CIL 实例模块，无本地委派，宿主关闭计数回零。相对于
  `~/.cache/ryupy/validation/stock-module-namespaces/` 的 JIT/目录 AOT 记录为
  `20261119T010804.355068Z.json`、`20261119T010804.350601Z.json`，OneFile 记录为
  `20261119T011500.557618Z.json`、`20261119T011500.550432Z.json`。默认 CLI 仍精确报
  E0100；原生异步差分 20 项、CPython 宿主回归 45 项和默认 `make check` 通过。
- 若将缺槽异常改为向原生 Task 抛出，再在模块级调用者捕获，功能结果相同，
  但关闭后仍报告一个 carrier/code-origin；用
  `tests/cases/stock_module_namespaces/pending/async_task_error_shutdown.py` 单独复现，
  不为兼容功能结果放宽已有产品门。
- FBA 固定原入口以候选 CLI 复跑越过 `auth.py:18`；新首阻塞是
  `backend/common/lifespan.py:9` 的 PEP 695 `type` 别名，另报告泛型类/函数
  注解作用域与 class import。原业务、框架回调识别、真实 DB 与搬迁均未通过。
