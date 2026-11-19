/* 真实祖先加载链：继承runpath与被加载消费者自己的runpath同时存在。 */
extern int fixture_relay(void);
int fixture_ancestor(void) { return fixture_relay(); }
