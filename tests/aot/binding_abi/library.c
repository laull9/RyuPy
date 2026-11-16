/* 自有 ABI 夹具：只做恒等、计数与字节读取；调用者始终拥有输入内存。 */
#include <stdint.h>
#include <stddef.h>
#ifdef _WIN32
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif
#define ECHO(name, type) API type echo_##name(type value) { return value; }
ECHO(i8, int8_t)
ECHO(u8, uint8_t)
ECHO(i16, int16_t)
ECHO(u16, uint16_t)
ECHO(i32, int32_t)
ECHO(u32, uint32_t)
ECHO(i64, int64_t)
ECHO(u64, uint64_t)
ECHO(isize, intptr_t)
ECHO(usize, uintptr_t)
ECHO(f32, float)
ECHO(f64, double)
ECHO(bool8, uint8_t)
static uint64_t calls;
API uint64_t call_count(void) { return calls; }
API uint8_t bad_bool(void) { return 2; }
API void empty(void) { ++calls; }
API uint64_t buffer(const uint8_t *data, size_t length) {
    /* 空缓冲也要求有效地址，长度内每一字节均可读。 */
    if (!data) return UINT64_MAX;
    uint64_t sum = length;
    for (size_t index = 0; index < length; ++index) sum += data[index];
    return sum;
}
API uint64_t utf8(const uint8_t *data, size_t length) { return buffer(data, length); }
API uint64_t bytes(const uint8_t *data, size_t length) { return buffer(data, length); }
API uint8_t bad_bool_buffer(const uint8_t *data, size_t length) {
    return buffer(data, length) == UINT64_MAX ? 0 : 2;
}
#ifndef OMIT_OPTIONAL
API uint64_t optional(const uint8_t *data, size_t length) { return buffer(data, length); }
#endif
API uint64_t mix(const uint8_t *text, size_t text_length,
                 const uint8_t *data, size_t data_length, int32_t value) {
    ++calls;
    return buffer(text, text_length) + buffer(data, data_length) + value;
}
API int64_t sixteen(int8_t a0, uint8_t a1, int16_t a2, uint16_t a3,
    int32_t a4, uint32_t a5, int64_t a6, uint64_t a7,
    intptr_t a8, uintptr_t a9, float a10, double a11,
    int32_t a12, int32_t a13, int32_t a14, int32_t a15) {
    return a0+a1+a2+a3+a4+a5+a6+a7+a8+a9+(int64_t)a10+(int64_t)a11+a12+a13+a14+a15;
}
/* 真实跨越寄存器/栈边界的最大签名：16 个逻辑缓冲，32 个机器参数。 */
#define BUFFER_ARG(index) const uint8_t *data##index, size_t length##index
#define BUFFER_VALUE(index) buffer(data##index, length##index)
API uint64_t sixteen_buffers(BUFFER_ARG(0), BUFFER_ARG(1), BUFFER_ARG(2), BUFFER_ARG(3),
    BUFFER_ARG(4), BUFFER_ARG(5), BUFFER_ARG(6), BUFFER_ARG(7),
    BUFFER_ARG(8), BUFFER_ARG(9), BUFFER_ARG(10), BUFFER_ARG(11),
    BUFFER_ARG(12), BUFFER_ARG(13), BUFFER_ARG(14), BUFFER_ARG(15)) {
    return BUFFER_VALUE(0)+BUFFER_VALUE(1)+BUFFER_VALUE(2)+BUFFER_VALUE(3)
        +BUFFER_VALUE(4)+BUFFER_VALUE(5)+BUFFER_VALUE(6)+BUFFER_VALUE(7)
        +BUFFER_VALUE(8)+BUFFER_VALUE(9)+BUFFER_VALUE(10)+BUFFER_VALUE(11)
        +BUFFER_VALUE(12)+BUFFER_VALUE(13)+BUFFER_VALUE(14)+BUFFER_VALUE(15);
}
