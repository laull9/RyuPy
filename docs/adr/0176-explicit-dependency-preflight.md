# ADR-0176：显式封闭快照的无条件声明依赖预检

## 问题与目标
真实 binaryornot consumer 缺少 chardet 时，旧构建仍可能进入源码发射甚至SDK发布。运行时才发现缺包既浪费构建时间，也使原生打包错误难以解释。此问题不是版本求解器或自动安装器的理由：源码可动态加载，分发元数据也不等于可导入模块、ABI或完整兼容性。

## 决定
在显式 `closed_snapshot` 请求增加可选 `dependency_check`：

```json
{
  "schema_version": 1,
  "mode": "closed_snapshot",
  "roots": [{"id": "vendor", "path": "vendor"}],
  "dependency_check": {
    "mode": "unconditional_closure",
    "requirements": ["binaryornot==0.4.4"]
  }
}
```

仅检查声明根所达的无条件传递依赖；不配置时现有自动发现、动态依赖及元数据载荷完全不变。使用锁定成熟库 `pep508_rs 0.9.2` 与 `pep440_rs 0.7.3` 解析要求及检查已安装版本，不手写 `>=` 比较，不联网、安装、选择候选或求解。

预检消费已做两次一致性检查的同一冻结Snapshot，不扫描另一套环境，不读取宿主site-packages。它位于源码图加载及SDK调用前，外层可创建自有构建尝试目录，但不能生成成功发布产物。错误包含请求方、原始requirement和缺失/不匹配原因。

## 元数据身份与有限范围
- dist-info读取 `METADATA` 的重复/折行 `Requires-Dist`；egg-info读取真实 `requires.txt`，不能只看PKG-INFO。双方同时给出非空依赖且权威不明确时硬拒绝，不随意合并。
- 所达分发的规范化Name必须与现有运行时发现键一致；重复/影子歧义拒绝，避免预检和运行时选择不同对象。未达分发的不支持依赖声明不阻止选定闭包；底层快照原有的文件安全和身份格式约束仍适用于整个显式根。
- 活跃基础依赖或根要求若使用markers、extras、direct URL，明确拒绝。不猜宿主/目标环境，不把缺乏实现的条件当false。
- egg-info中 `[extra]` / `[extra:marker]` 属于本模式未请求的extra，可保留但不遍历；`[:marker]` 是基础条件依赖，必须拒绝，不能伪装成未选extra。
- 非法要求、解析警告和无法解析为PEP440的版本不能算满足。`===` 任意字符串等号明确拒绝，避免成熟库的规范化行为与原始字符串等价语义不符。所达分发必须有单一已支持的Metadata-Version；未知格式不能当成空依赖。
- 闭包有明确大小预算；循环终止，但同一分发由不同路径收到的每个版本约束均需检查。

## 收据与性能边界
成功时额外写编译期 `dependency-check.json`，绑定请求字节SHA256、既有metadata载荷SHA256、原根要求、所达名称和逐条检查。收据不是运行时authority，不修改已有metadata schema，也不加入Native dist。会话复用时清空旧结果，失败不能留下本次成功收据。

此检查仅证明选定的无条件声明依赖在封闭快照中存在且版本匹配；不检查Requires-Python或目标Python版本兼容性，不证明源码导入、二进制扩展ABI、任意动态插件或Nuitka级全面兼容。编译期开销与缺包失败耗时单独记录；不以预检耗时冒充Native运行性能改进。

## 验收计划
成熟解析边界、身份歧义、缺包、版本冲突、循环、多路径约束、折行头、未选extra与基础条件段均需Rust合同。真实binaryornot/chardet正例仍须公开AOT构建、47来源审计、原位/搬迁宿主8次和禁网无Python/.NET容器8次。负例以无效源码和假SDK哨兵证明预检先于源码/发布，保留完整原流；默认关闭仍保旧行为。通过前保持候选状态，不把计划计为已验。
