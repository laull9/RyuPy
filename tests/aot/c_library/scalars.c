/* 精确标量C ABI竖切：调用计数证明拒绝输入在进入C之前完成，buffer保留长度与原字节。 */
#include <stdint.h>
#include <stddef.h>
static int32_t calls;
static int32_t last;
#define ECHO(name, type) type echo_##name(type value) { calls++; return value; }
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
uint64_t measure_data(const uint8_t *data, size_t length) {
    calls++;
    uint64_t result = length;
    for (size_t index = 0; index < length; index++) result = result * 131 + data[index];
    return result;
}
uint64_t measure_utf8(const uint8_t *data, size_t length) { return measure_data(data, length); }
void touch(int32_t value) { calls++; last = value; }
int32_t last_value(void) { return last; }
int32_t calls_count(void) { return calls; }
