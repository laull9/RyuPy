# ADR-0058：有限通用 shutil.copyfileobj

状态：本地专项与完整门禁通过，尚未发布。

## 动机与范围

2026-11-03 在资源候选 d149ac1（含未发布 float、bool、资源切）上对锁定
pyfiglet 1.0.2 原固定入口进行一次 CP参考、CLI编译及JIT执行。CP输出与原golden
一致，编译成功，JIT首败为 `ModuleNotFoundError: No module named 'shutil'`。
这不是公开 main 的新结果，也未到字体资源读取。保留首失败，不推断后续模块已通过。

实际项目的 `shutil.copyfileobj` 和 `copy` 调用在安装字体分支，固定渲染入口仅先导入
该模块。本切提供真实通用流复制，不以空模块或虚假成功跨过导入；不保证项目因此通过。
`copy`、`copyfile`、`copymode`、`rmtree` 等未实现，不导出假函数；[真实 CP 正例](../../tests/fixtures/shutil_copyfileobj/copy.py.pending)另保pending，不计通过。

## 协议与设计

- 公开 `copyfileobj(fsrc, fdst, length=0)`，三个参数均可用关键字；绑定成功才访问流。
- 对 length 求真值一次；假值读取当前模块 `COPY_BUFSIZE`。Windows 默认1MiB，
  其它平台64KiB；保存的函数别名同样看见模块属性重绑，不冻结定义时常量。
- 顺序取得并缓存普通属性 `fsrc.read`、`fdst.write` 各一次，不改成特殊方法查找。
- 每轮调用 `read(length)`，对返回对象求真值；真值时把同一对象交给 `write`，忽略
  write 返回值。短读不提前结束，负length仍按相同循环，不假定只读一次。
- 不转换任意 length 或 buffer 的类型，不重放 read，不补 flush/close/seek，不把已有
  写入回滚。属性读取、读、真值及写的用户异常沿现有调用路径传播，不增加通用catch。
- 复用现有签名绑定、普通属性、CallValue/Truthy 与 io 实现，无新 Rust/CIL ABI。

## 边界与成本

普通流按块处理，不先读取整文件；工作和调用次数随实际流协议线性增长。负length或
自定义read可返回全量数据，故不承诺固定总内存或零分配；恶意无限真值流也不新增人工
循环上限。缓存方法只减少每轮重复属性查找，不声称性能测量收益。

未实现的路径复制还涉及目标目录、samefile/硬链接、symlink、FIFO、权限及部分失败等，
不能由 File.Copy 或本API简单包装冒充完整语义。通用错误现场与其它io长尾沿现有边界。

## 验证计划与状态

固定 CPython3.12.13 参考已执行，C#协议专项首次10/10通过；首轮实际CP/JIT driver为3/4。
唯一失败再次暴露已登记的nested-caught现场旧债：函数内部已捕获的context异常多带外层
module帧，copyfileobj用户错误的剩余链/帧一致。[原CP完整期望](../../tests/fixtures/shutil_copyfileobj/nested_context_frames.py.pending)保pending，不修改全局
traceback，也不放宽当前帧断言；活跃case迁到模块级context后调用transfer，保持完整用户链及精确库帧投影；第二次driver4/4通过。
完整正常make check首次通过：661项C#、228项CP基线、202项e2e及30项既有ignored；
fmt/严格clippy/workspace与cases自检全部通过，无快照迁移。覆盖绑定和副作用顺序、模块重绑、方法缓存、对象身份、短读及负长度、
用户异常、BytesIO/StringIO/真实文件位置和无关闭/刷新行为。完整原始错误流保留；原生实现
没有 CP 的 shutil.py 库帧，若比较用户栈须仅精确排除已验证的该库实现帧并保留其存在证据。
不新增 AOT、项目矩阵或性能运行，历史5/20与matmul禁分层回归不因此改变。
