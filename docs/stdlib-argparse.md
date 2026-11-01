# argparse：常用单值命令行

原生实现真实解析 token，不按测试参数生成固定结果；不引入宿主进程退出或宿主命令行解析器。

## 当前 API

- `ArgumentParser`：prog、usage、description、epilog、add_help、allow_abbrev。prog 缺省取当前 sys.argv[0] 的文件名。当前构造签名是这六个参数的紧凑子集，其他 CPython 构造参数不支持；建议除 prog 外用关键字。
- `add_argument`：单个位置参数，或一组短/长选项别名。支持 action=`store`/`store_true`/`store_false`，type、default、required（仅选项）、help、metavar、choices、dest（仅选项）。返回只读基础 Action 元数据；不提供自定义 Action 类或 action 注册。
- `parse_args(args=None, namespace=None)`：缺省读取当前 sys.argv[1:]；显式 args 通过迭代器读取，元素须字符串。namespace 限本模块 Namespace，不承诺任意用户属性对象。
- `Namespace(**kwargs)`：可变属性、活动 `__dict__`；不承诺 CPython Namespace 的专用 repr/相等/删除接口。
- `format_usage`、`format_help` 返回文本；print_usage/print_help 接受 file，缺省写当前 stdout。
- `error(message)`：当前 stderr 上先 usage 再错误消息，抛 SystemExit(2)。`exit(status=0,message=None)` 可先写当前 stderr 再抛 SystemExit(status)。没有调用 Environment.Exit。

## 解析与错误顺序

选项支持 `--name=value`、短选项紧贴值、布尔短选项组合、`--` 结束选项、常用负整/小数值。唯一长选项前缀在 allow_abbrev 为真时可用，歧义报错。位置参数与选项可交错；重复选项保留最后值。

转换器在遇到参数时调用；ValueError/TypeError 转为参数错误，其他 Python 异常正常传播。choices 使用 Python 成员协议。未出现参数的字符串默认值在完成必填检查后转换，非字符串默认值不转换；用户提供 namespace 的已有属性不被缺省值覆盖。

帮助选项即时写帮助并抛 SystemExit(0)。缺参、未知项、类型错误写 stderr 并抛 SystemExit(2)；流 write 抛错不会被吞成成功退出。普通解析不读写标准流。

## 明确范围

nargs、append/count/custom action、子解析器、参数组、互斥组、parse_known_args、parse_intermixed_args、文件参数扩展、非 `-` 前缀、formatter_class、SUPPRESS、exit_on_error 尚未实现。未知 add_argument 选项明确 NotImplementedError；未知构造关键字由绑定器拒绝。

help/usage 提供默认短文本布局及位置/选项分组、metavar/choices 显示，help 插值支持 prog/default；usage 支持 prog。长行自动折行、终端宽度、其他帮助插值/自定义格式器未实现；不能据短文本通过宣称完整帮助格式兼容。选项定义冲突目前 ValueError，而非完整 ArgumentError 类型族；对应类尚不暴露。

## 成本与证据

参数定义保存元数据；每次 parse_args 物化输入字符串列表，按参数表线性查找（约 O(token 数×定义数)），不缓存用户 type/choices 的结果。适用于常见小型 CLI；暂无大规模选项表性能主张。

差分 `m3_7_argparse.py` 覆盖位置/类型/默认/缩写/负值/结束选项、帮助和五种退出、当前 sys.argv 与标准流重绑定；原 `m3_7_cli_logging.py` 保留作为跨模块验收。专测 `PyArgparseTests` 覆盖组合短选项、元数据生成帮助、已有 namespace 与字符串默认值。
