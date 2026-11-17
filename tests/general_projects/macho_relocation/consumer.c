/* 真实动态加载边：调用另一映像中的符号，以运行结果核对重定位选择的版本。 */
extern int fixture_value(void);
int fixture_relay(void) { return fixture_value(); }
