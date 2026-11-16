/* 自有 C ABI 入口：只读借用传给独立依赖库，验证相邻动态依赖闭包。 */
#include <stddef.h>
#include <stdint.h>
extern uint64_t helper_measure(const uint8_t *, size_t);
uint64_t measure(const uint8_t *data, size_t length) { return helper_measure(data, length); }
int32_t add(int32_t left, int32_t right) { return left + right; }
/* 真实系统 C 库 abs 经标准 C 入口绑定；不同平台的导出修饰由 C 编译器负责。 */
#include <stdlib.h>
int32_t absolute(int32_t value) { return abs(value); }
