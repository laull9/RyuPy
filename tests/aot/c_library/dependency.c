/* 依赖库夹具：包含零字节和高位字节的长度/内容读取，用于核对借用边界。 */
#include <stddef.h>
#include <stdint.h>
uint64_t helper_measure(const uint8_t *data, size_t length) {
    uint64_t result = length;
    for (size_t i = 0; i < length; ++i) result += data[i];
    return result;
}
