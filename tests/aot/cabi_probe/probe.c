/* 自有定宽 C ABI 探针；不是 Python 扩展或通用绑定层。 */
#include <stdint.h>
#ifdef RYUPY_SHARED
# ifdef _WIN32
#  define EXPORT __declspec(dllexport)
# else
#  define EXPORT __attribute__((visibility("default")))
# endif
#else
# define EXPORT
#endif
EXPORT int64_t ryupy_demo_add_i32(int32_t left, int32_t right) {
    return (int64_t)left + (int64_t)right;
}
#ifdef RYUPY_REFERENCE
#include <stdio.h>
#include <inttypes.h>
int main(void) {
    const int32_t inputs[5][2] = {{0,0},{-1,4},{INT32_MAX,INT32_MAX},
                                {INT32_MIN,INT32_MIN},{INT32_MIN,INT32_MAX}};
    for (int i=0; i<5; ++i)
        printf("%" PRId64 "\n", ryupy_demo_add_i32(inputs[i][0], inputs[i][1]));
    return 0;
}
#endif
