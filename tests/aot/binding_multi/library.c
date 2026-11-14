/* 自有多导出C库：所有运算先扩为int64，测试极值时不触发signed32溢出。 */
#include <stdint.h>
#if defined(_WIN32)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif
EXPORT int64_t demo_add(int32_t left, int32_t right) {
    return (int64_t)left + (int64_t)right;
}
#ifndef OMIT_SUBTRACT
EXPORT int64_t demo_subtract(int32_t left, int32_t right) {
    return (int64_t)left - (int64_t)right;
}
#endif
