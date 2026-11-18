/* 独立Mach-O依赖输入：调用方以构建宏指定值，不读取运行环境或产品名称。 */
#ifndef FIXTURE_VALUE
#error "构建器必须明确提供独立依赖的返回值"
#endif
int fixture_value(void) { return FIXTURE_VALUE; }
