# 真实标准输出 detach 有限协议

真实进程 stdout/stderr 的稳定 binary buffer、detach前flush、旧wrapper失效、重绑binary后的退出flush与close边界；UTF8与NUL按字节保留。任意非UTF8字节另由C# raw carrier及外部byte-exact图验证，不把二进制数据解码再写文本。

stdin已有文本预读、宿主TextWriter-only、磁盘TextIOWrapper.detach不在本切范围，明确NI。此能力不意味着完整io.BufferedWriter（seek/read/raw/fileno等未扩）。用户借用OS句柄不由逻辑close销毁，但本carrier与关联wrapper必须拒绝后续写入。

完整状态与所有权说明见 [ADR-0184](../../../docs/adr/0184-standard-output-detach.md)。无改绑 detach 的退出负图实际产物为120，沿用既有stderr报告前缀偏差，未混入这里的逐字节正合同。CLI run仍将非零子进程状态归一为1。
