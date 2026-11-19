# ADR-0054：abs 完整求值与单参数绑定

- 状态：已进入 main，Ubuntu/macOS 普通 CI 通过
- 基线：int修复提交 `0f52ec5037edbe7094c96d55485ac0c591d14862`

## 已有错误

真实CLI复现 `abs(-3, extra())`：固定CPython3.12.13先执行extra的打印，再抛TypeError；旧RyuPy却退出0输出3。MIR旧 `!has_keyword` 守卫只读取第一个参数，遗漏额外参数/星号，ty另把零参错误提前作为编译诊断。运行时已有严格单参数校验，无需新增abs协议或ABI。

## 窄修与性能边界

仅一个普通位置参数保现int/float静态快路；其余形态沿既有完整求值、展开和DynBuiltin，绑定成功才执行一次 `__abs__`，保留原返回对象身份。零参在运行时抛TypeError，额外参数/关键字的表达式均执行，不把不合法调用优化成成功。

abs专属冷helper只对Single/关键字绑定错误记录caller/context，用户协议正文在catch外，已有异常身份/帧不替换。DynAbs末尾自行产生的badoperand错误在该抛点记录现场。通用CallBuiltin、CallValue、FlattenArgs不改，无新增正常入口name guard、EH区域或数组，也不因这次正确性修复声称已测加速。

无效星号展开在callee之前发生；保存的一等abs也经过通用CallValue提前展开。其错误类型和副作用仍保留，但通用错误现场的旧边界不在本切重做。[完整CP原期望](../../tests/fixtures/abs_constructor/invalid_star_frames.py.pending)单独保留，不计通过。有效星号展开后的零/一/多参数仍进入真实abs绑定，本切必须验证。

不修改int、float、str、type等其它调用契约；静态I64数值宽度边界保持。项目通过率仍为历史固定入口记录，未运行新全项目或AOT/性能组。

## 验证

固定CP13核心及错误链参考已保存；ty/MIR结构专项各1项通过，验证完整副作用顺序与单参数Int/Float静态快路。runtime 8/8、真实driver 3/3均首次运行通过，覆盖完整输出、8行异常链投影与MIR结构。正常完整门禁首次通过：611项C#、228项CP基线、202项e2e通过与30项ignored，workspace、fmt和strict clippy均通过，未更新快照。重新构建CLI后，最初多参复现与CP原始stdout逐字相同，均先输出extra-evaluated再输出arity-rejected，stderr为空。未将pending算作通过，未运行新的原生或性能组。


## 普通 CI 远端验收

2026-11-03，int 与 abs 合批经用户批准进入 main `d4d8243a34bc8466eefb6995edfde42f3bf87eb5`；连接器生成的提交元数据不同，但树与本地已验候选完全相同。[普通 CI 41620972640](https://github.com/laull9/RyuPy/actions/runs/41620972640) 的 Ubuntu/macOS 两项均成功。两份官方日志分别确认 611 项 C#、228 项 CP 基线、202 项 e2e 通过及 30 项 ignored，int/abs 各 3 项真实 driver 均通过。未新增原生、项目矩阵或性能测量，原 pending 与 matmul 禁分层回归仍保留。
