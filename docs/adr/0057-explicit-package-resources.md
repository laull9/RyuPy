# ADR-0057：显式模块包含与有限文件包资源

状态：本地候选，专项与完整门禁通过；尚未发布。不把导入成功计为项目通过。

## 动机与范围

2026-11-03 曾以公开 main d4d8243 对锁定 pyfiglet 1.0.2 的原固定入口做一次
CP参考、一次CLI编译及一次JIT运行。CP输出与历史golden一致，编译成功，JIT首先报
`ModuleNotFoundError: No module named 'importlib'`。这是已审核的历史单入口结果，
不是20项目重测；环境恢复后没有重新运行该项目，也不把其它源码风险当作实测失败。

真实字体读取还需要字符串包锚点、文件查找/读取、ZIP检查和文本解码等。
本切交付通用显式依赖和文件包资源，不硬编码项目名、不修改第三方入口、不用空模块
或恒定ZIP判断绕过行为，不保证 pyfiglet 因此通过。

## 决策

- `build` / `run` 增加可重复 `--include-module NAME`，通过现有搜索根和resolver纳入
  真实源码模块及父包/静态依赖。包含只决定编译闭包，不提前执行包初始化、不自动复制数据。
  首切选项名限ASCII标识符组成的绝对点分名；缺失、非法路径、非包父节点以及首段
  `__main__` 明确构建失败。重复声明去重；旧源码Unicode导入与普通native fallback不变。
- resolver的实际Package分支保留为跨层冷metadata，不从`__init__.py`文件名或可改
  `__package__`推测。仅真实包使用新增十参`RegisterResourceLocated`；非包仍走原九参
  `RegisterLocated`，旧dump保原登记调用。新增MemberRef会改变PE元数据，不承诺旧PE字节不变。
- 原生`importlib.resources.files(anchor)`接受明确包名或实际模块对象、单位置或anchor关键字。
  字符串正常Import，保持父包、缓存、循环中对象身份及失败重试；返回现有真实PyPath，
  复用joinpath/exists/open/read/seek等文件能力。父importlib不伪造未实现的import_module等API。
- 资源能力绑定每次实际PyModule对象和不可变初始位置。同名重登记不改变旧对象根；
  公开`__file__`、`__package__`、`__name__`重绑不用于猜测根。显式暴露`__spec__`或
  `__loader__`时明确拒绝，因为本切没有通用ModuleSpec/loader协议。
- JIT只用真实sourceabsolute锚点；AOT只用现有显式资源声明选中的可执行文件相对锚点。
  虚拟`__init__.py`不必在交付目录中存在。未选AOT/legacy不得回退开发机源码或cwd。
  `--resources`仍仅AOT可用，路径/冻结预算/部署机制不变；include选项本身不部署资源。

## 边界与成本

非包module anchor、默认caller/None、弃用package关键字所需warning、namespace、zip及
custom loader等合法长尾尚未支持，明确NotImplementedError，不伪装CP错误或空目录。
已保留[真实CP正期望](../../tests/fixtures/package_resources/files_extended.py.pending)，
其中默认caller在有真实spec的已导入辅助模块中调用，不拿`__main__.spec=None`失败当正例。
该pending不加入本切passed或ignored分母。不同无效调用的完整CP错误域不据此宣称完成。

仅冷模块登记新增能力描述及弱表关联；普通数值/调用热路不变。files按实际包查询并分配
路径对象，不缓存目录内容，不扫描整个宿主文件系统或默认打包全部资源。返回的是既有
文件系统Path，不新增运行时沙箱/符号链接/父路径限制承诺；`_internal`是外置资源而非嵌入资源。

## 本轮验证

- 固定CPython3.12.13参考核绑定错误优先级、循环包、metadata重绑和字节流读取；
  三个自有包fixture及长尾pending的CP先验成功。没有新增第三方项目执行。
- C#资源专项12/12；loader/Session6、CLI2、新PE2及原定位PE3均首次通过。
- driver4/4：三项实际CP/JIT完整原双流差分覆盖双包同名资源、外cwd及file重绑、
  字符串anchor惰性初始化/循环、失败重试；另一项锁缺失include构建失败且旧哨兵不变。
- AOT目前只由定位与真实PE/既有资源机制的测试约束，未进行本切原生发布或搬迁执行。
  完整正常make check首次在新driver测试的3处严格clippy规则停止（两处Default后赋值、
  一处err().expect）；仅测试初始化/取错误写法窄修，保全部断言。第二次正常make check
  通过：651项C#、228项CP基线、202项e2e及30项既有ignored，fmt/严格clippy/workspace全绿，
  无快照迁移。另以实际公开CLI run配重复include从外cwd执行cycle fixture一次，
  stdout逐字等于固定CP参考、stderr空、exit0；不是第三方项目或AOT验收。
  无新增性能测量，matmul禁分层历史回归仍开放。
