/* 在同一进程依次加载多个消费者；RTLD_LOCAL保留独立句柄并暴露跨版本误绑定。 */
#include <dlfcn.h>
#include <stdio.h>

int main(int argc, char **argv) {
    for (int i = 1; i < argc; ++i) {
        void *handle = dlopen(argv[i], RTLD_NOW | RTLD_LOCAL);
        if (!handle) { fprintf(stderr, "%s\n", dlerror()); return 2; }
        int (*relay)(void) = (int (*)(void))dlsym(handle, "fixture_relay");
        if (!relay) { fprintf(stderr, "%s\n", dlerror()); return 3; }
        printf("%d\n", relay());
        /* 有意保留句柄到进程退出，让后一个加载边必须面对已加载的另一版本。 */
    }
    return 0;
}
