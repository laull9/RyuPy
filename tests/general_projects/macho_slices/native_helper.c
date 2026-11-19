/* 无扩展名的实际可执行消费者：目标slice由真实机器指令和进程加载器共同验收。 */
#include <stdio.h>
int main(void) {
#if defined(__arm64__) || defined(__aarch64__)
    puts("native-helper-arm64");
#else
    puts("native-helper-x86_64");
#endif
    return 0;
}
