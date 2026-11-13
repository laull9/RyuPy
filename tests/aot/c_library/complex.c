/* ctypes 通用路径的布局、返回指针、回调及成对分配/释放合同。 */
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
typedef struct { int32_t x; double y; } Pair;
static int32_t active;
Pair pair_transform(Pair value) { value.x += 7; value.y *= 2.0; return value; }
uint8_t *buffer_create(const uint8_t *source, size_t length) {
    uint8_t *result = malloc(length ? length : 1);
    if (result) { if (length) memcpy(result, source, length); ++active; }
    return result;
}
void buffer_release(uint8_t *value) { if (value) { free(value); --active; } }
int32_t active_buffers(void) { return active; }
int32_t callback_apply(int32_t (*callback)(int32_t), int32_t value) { return callback(value) + 1; }
