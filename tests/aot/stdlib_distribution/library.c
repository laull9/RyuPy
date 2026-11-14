/* 自有 C ABI 夹具：只借用调用期字节，不分配内存。 */
#include <stddef.h>
#include <stdint.h>
#if defined(_WIN32)
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif
API uint64_t probe_sum_bytes(const uint8_t *data, size_t length) {
    uint64_t result = 0;
    for (size_t i = 0; i < length; ++i) result += data[i];
    return result;
}
