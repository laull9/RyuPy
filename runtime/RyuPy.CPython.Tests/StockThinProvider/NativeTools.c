/* 独立测试控制面：占满公开 watcher 槽位，证明产品 GC owner 不占用 watcher 资源。
 * 此动态库只由仓外 Runner 构建，产品 shim/CLI 不携带这些导出；不复活 cleared F。
 */
#include <Python.h>
#if defined(_WIN32)
#define TEST_API __declspec(dllexport)
#else
#define TEST_API __attribute__((visibility("default")))
#endif
static int slots[8], slot_count;
static int noop(PyFunction_WatchEvent event, PyFunctionObject *f, PyObject *value) {
    (void)event; (void)f; (void)value; return 0;
}
TEST_API int thin_test_reserve(void) {
    while (slot_count < 8) {
        int id = PyFunction_AddWatcher(noop);
        if (id < 0) { PyErr_Clear(); break; }
        slots[slot_count++] = id;
    }
    return slot_count;
}
TEST_API int thin_test_free_slots(void) {
    int count = slot_count;
    while (slot_count) if (PyFunction_ClearWatcher(slots[--slot_count]) < 0) return -1;
    return count;
}
