# 标准输入 CR 行末不再预读下一行

状态：独立main基线修复，局部与完整门禁已通过；修后真实macOS PTY尚待固定CI。
这不是I64性能候选，也不声称用户原始1.py已完整复现或修复。

## 缺陷证据

旧main `457886017cd4d32911a09b09307d07c327b87192` 在Linux与
[macOS arm64诊断CI](https://github.com/laull9/RyuPy/actions/runs/41413991719)
的摘要重建猜数候选中：pipe与LF控制流正常，但逐提示发送CR时第二输入后停等并超时。
mac runner为26.6.2/arm64，不代表用户具体硬件；原1.py仍未取得。

独立.NET10.0.14探针在Linux实际读到字符100(d)、13(CR)，进入Peek后停等；
追加x仍不返回，追加LF后Peek返回120(x)。这证明旧PyStandardStream.Read的CR→Peek
可以等待下一整行，不是整数计算证据。macOS诊断原始转录保CI artifact（14天），不入Git。Linux独立探针属于本地诊断，
未将其原始日志写入仓库。
立即输入的首字符额外回显是另一显示时序差异，本修不声称解决它。

## 修复与已验边界

每个标准流记录一个待吞LF状态：消费CR即输出换行，下一次实际读取才吞可选LF；
非LF同轮处理而不丢失。read(0)不读取或改变状态，底层Read抛错保留状态，EOF正确结束。
不针对Console写特殊分支，不修改termios，也不扩大现有Unicode代理项处理。

11项确定性C#专项通过：禁止CR后Peek/越界读、CRLF/CRCRLF、非LF后继、size0/有限size、
EOF、读取异常恢复、迭代及input提示flush。独立main树完整make check退出0：
445 C#、225固定CP基线、199 e2e通过/30 ignored，fmt/clippy/Rust门禁通过，无快照更新。
修后将用同源、同输入、同预算的macOS管道/PTY诊断验证，不更改期望来掩盖错误。
