/* RyuPy 严格零字节码内核 C4-1 纯 C 解释器侧表、冻结注册表与校验辅助。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_registry.h`，
 * 由 `Python/import.c` 顶部在 `Python/ryupy_module_locks.h` 之前包含，在不修改
 * `PyInterpreterState` 与 `struct _import_state` 任何既有布局的前提下，提供：
 * 1. 纯 C 解释器侧表（`RpSideTableSlot`）：以 `(interp, interp_id)` 为键，由独立短互斥锁
 *    `rp_side_table_mutex` 保护（支持 PEP 684 独立 GIL 子解释器并发），锁内仅做指针
 *    查/插/摘，绝不调用 Python C API 或获取 GIL；带 `finalizing` 墓碑防止关机重建。
 * 2. 解释器/线程归属校验：明确仅支持已初始化解释器及所属 GIL 线程；初始化前调用返回
 *    纯 C `-1` 不触碰 `PyErr_*`。
 * 3. 纯 C UTF-8、合法点分模块名与内嵌 NUL 字节拒绝，防止 C 字符串查表截断碰撞。
 */
#ifndef Py_RYUPY_IMPORT_REGISTRY_H
#define Py_RYUPY_IMPORT_REGISTRY_H

#include <stdlib.h>
#include <string.h>

typedef struct {
    int kind;
    int is_package;
    char *name;
    char *origin;
    char *package_dir;
} RpFrozenEntry;

typedef struct RpModuleLockNode {
    char *name;
    int has_owner;
    unsigned long owner_tid;
    int recursion_count;
    int waiters;
    size_t refcount;
    PyCOND_T cond;
    struct RpModuleLockNode *next;
} RpModuleLockNode;

typedef struct {
    unsigned long tid;
    RpModuleLockNode **locks;
    size_t count;
    size_t capacity;
} RpBlockedThread;

typedef struct {
    RpFrozenEntry *entries;
    size_t entry_count;
    size_t entry_capacity;
    int frozen;
    int has_cil_callbacks;
    int cil_cleanup_in_progress;
    RpKernelCilImportCallbacks cil_callbacks;
    rp_kernel_fini_fence_fn fini_fence_fn;
    void *fini_fence_user_data;

    /* 保护 locks / blocked / finalizing / active_waiters 的短临界区互斥锁；
     * 严禁在持锁期间调用 Python API 或获取 GIL。 */
    PyMUTEX_T table_mutex;
    PyCOND_T drain_cond;
    int sync_initialized;
    int finalizing;
    size_t active_waiters;
    RpModuleLockNode *locks;
    RpBlockedThread *blocked;
    size_t blocked_count;
    size_t blocked_capacity;

    /* 纯 C ModuleSpec 与 Loader 堆类型，随解释器生命周期在 _PyImport_FiniCore 释放。 */
    PyTypeObject *spec_type;
    PyTypeObject *builtin_loader_type;
    PyTypeObject *extension_loader_type;
    PyTypeObject *cil_loader_type;
} RpKernelImportState;

typedef struct RpSideTableSlot {
    PyInterpreterState *interp;
    int64_t interp_id;
    int finalizing;
    RpKernelImportState *state;
    struct RpSideTableSlot *next;
} RpSideTableSlot;

static PyMUTEX_T rp_side_table_mutex;
static int rp_side_table_mutex_ready = 0;
static RpSideTableSlot *rp_side_table_head = NULL;

static void
rp_kernel_side_table_init(void)
{
    if (!rp_side_table_mutex_ready) {
        if (PyMUTEX_INIT(&rp_side_table_mutex) == 0) {
            rp_side_table_mutex_ready = 1;
        }
    }
}

static void
rp_kernel_side_table_fini(void)
{
    if (rp_side_table_mutex_ready && rp_side_table_head == NULL) {
        PyMUTEX_FINI(&rp_side_table_mutex);
        rp_side_table_mutex_ready = 0;
    }
}

static char *
rp_raw_strdup(const char *src)
{
    if (src == NULL) {
        return NULL;
    }
    size_t len = strlen(src);
    char *dst = (char *)PyMem_RawMalloc(len + 1);
    if (dst == NULL) {
        return NULL;
    }
    memcpy(dst, src, len + 1);
    return dst;
}

static int
rp_verify_interp_ownership(PyInterpreterState *interp, PyThreadState **out_tstate)
{
    /* 初始化前或已完成关机后严禁触碰 tstate / PyErr_*，直接纯 C 返回 -1。 */
    if (!Py_IsInitialized()) {
        return -1;
    }
    PyThreadState *tstate = _PyThreadState_GetCurrent();
    if (tstate == NULL) {
        return -1;
    }
    if (interp == NULL) {
        PyErr_SetString(PyExc_ValueError, "target PyInterpreterState must not be NULL");
        return -1;
    }
    if (tstate->interp != interp) {
        PyErr_SetString(PyExc_RuntimeError,
                        "RyuPy import registry API requires active GIL thread bound to target interpreter");
        return -1;
    }
    if (out_tstate != NULL) {
        *out_tstate = tstate;
    }
    return 0;
}

static RpSideTableSlot *
rp_side_table_find_locked(PyInterpreterState *interp, int64_t interp_id)
{
    for (RpSideTableSlot *s = rp_side_table_head; s != NULL; s = s->next) {
        if (s->interp == interp && s->interp_id == interp_id) {
            return s;
        }
    }
    return NULL;
}

static RpKernelImportState *
rp_import_get_state(PyInterpreterState *interp, int create)
{
    if (interp == NULL || !rp_side_table_mutex_ready) {
        return NULL;
    }
    int64_t interp_id = interp->id;
    RpKernelImportState *new_state = NULL;
    RpSideTableSlot *new_slot = NULL;

    PyMUTEX_LOCK(&rp_side_table_mutex);
    RpSideTableSlot *slot = rp_side_table_find_locked(interp, interp_id);
    if (slot != NULL) {
        RpKernelImportState *res = slot->finalizing ? NULL : slot->state;
        PyMUTEX_UNLOCK(&rp_side_table_mutex);
        return res;
    }
    if (!create) {
        PyMUTEX_UNLOCK(&rp_side_table_mutex);
        return NULL;
    }
    PyMUTEX_UNLOCK(&rp_side_table_mutex);

    /* 在短表锁外检查目标解释器是否已进入 finalizing（如 Py_FinalizeEx 关机阶段或 Py_EndInterpreter）；
     * atexit 回调运行于置位 _finalizing 之前，仍可正常创建/使用侧表；置位后禁止新建游离 state。 */
    if (_PyInterpreterState_GetFinalizing(interp) != NULL) {
        PyErr_SetString(PyExc_RuntimeError,
                        "cannot initialize RyuPy import registry while interpreter is finalizing");
        return NULL;
    }

    new_state = (RpKernelImportState *)PyMem_RawCalloc(1, sizeof(RpKernelImportState));
    new_slot = (RpSideTableSlot *)PyMem_RawCalloc(1, sizeof(RpSideTableSlot));
    if (new_state == NULL || new_slot == NULL) {
        PyMem_RawFree(new_state);
        PyMem_RawFree(new_slot);
        PyErr_NoMemory();
        return NULL;
    }
    if (PyMUTEX_INIT(&new_state->table_mutex) != 0) {
        PyMem_RawFree(new_state);
        PyMem_RawFree(new_slot);
        PyErr_SetString(PyExc_RuntimeError, "failed to initialize RyuPy module lock primitives");
        return NULL;
    }
    if (PyCOND_INIT(&new_state->drain_cond) != 0) {
        PyMUTEX_FINI(&new_state->table_mutex);
        PyMem_RawFree(new_state);
        PyMem_RawFree(new_slot);
        PyErr_SetString(PyExc_RuntimeError, "failed to initialize RyuPy module lock primitives");
        return NULL;
    }
    new_state->sync_initialized = 1;
    new_slot->interp = interp;
    new_slot->interp_id = interp_id;
    new_slot->state = new_state;

    PyMUTEX_LOCK(&rp_side_table_mutex);
    slot = rp_side_table_find_locked(interp, interp_id);
    int interp_finalizing = (_PyInterpreterState_GetFinalizing(interp) != NULL);
    if (slot != NULL || interp_finalizing) {
        RpKernelImportState *existing = (slot != NULL && !slot->finalizing) ? slot->state : NULL;
        PyMUTEX_UNLOCK(&rp_side_table_mutex);
        PyCOND_FINI(&new_state->drain_cond);
        PyMUTEX_FINI(&new_state->table_mutex);
        PyMem_RawFree(new_state);
        PyMem_RawFree(new_slot);
        if (slot == NULL && interp_finalizing) {
            PyErr_SetString(PyExc_RuntimeError,
                            "cannot initialize RyuPy import registry while interpreter is finalizing");
        }
        return existing;
    }
    new_slot->next = rp_side_table_head;
    rp_side_table_head = new_slot;
    PyMUTEX_UNLOCK(&rp_side_table_mutex);
    return new_state;
}

static const RpFrozenEntry *
rp_import_find_entry(const RpKernelImportState *state, const char *name)
{
    if (state == NULL || name == NULL) {
        return NULL;
    }
    for (size_t i = 0; i < state->entry_count; i++) {
        if (strcmp(state->entries[i].name, name) == 0) {
            return &state->entries[i];
        }
    }
    return NULL;
}

/* 纯 C UTF-8 与点分模块名合法性校验（不依赖 Python 堆与 PyErr_*）。 */
static int
rp_is_valid_utf8_cstr(const char *s)
{
    if (s == NULL) {
        return 0;
    }
    const unsigned char *p = (const unsigned char *)s;
    while (*p != '\0') {
        unsigned char c = *p;
        if (c < 0x80u) {
            p++;
        }
        else if ((c & 0xE0u) == 0xC0u) {
            if (c < 0xC2u || (p[1] & 0xC0u) != 0x80u) return 0;
            p += 2;
        }
        else if ((c & 0xF0u) == 0xE0u) {
            unsigned char c1 = p[1], c2 = p[2];
            if ((c1 & 0xC0u) != 0x80u || (c2 & 0xC0u) != 0x80u) return 0;
            if (c == 0xE0u && c1 < 0xA0u) return 0;
            if (c == 0xEDu && c1 >= 0xA0u) return 0;
            p += 3;
        }
        else if ((c & 0xF8u) == 0xF0u) {
            unsigned char c1 = p[1], c2 = p[2], c3 = p[3];
            if ((c1 & 0xC0u) != 0x80u || (c2 & 0xC0u) != 0x80u || (c3 & 0xC0u) != 0x80u) return 0;
            if (c == 0xF0u && c1 < 0x90u) return 0;
            if (c > 0xF4u || (c == 0xF4u && c1 > 0x8Fu)) return 0;
            p += 4;
        }
        else {
            return 0;
        }
    }
    return 1;
}

static int
rp_validate_module_name(const char *name)
{
    if (name == NULL || name[0] == '\0' || name[0] == '.') {
        return -1;
    }
    for (const char *p = name; *p != '\0'; p++) {
        if (*p == '.' && (*(p + 1) == '.' || *(p + 1) == '\0')) {
            return -1;
        }
    }
    return rp_is_valid_utf8_cstr(name) ? 0 : -1;
}

/* 提取模块名 UTF-8 并拦截内嵌 NUL 字节，防止 C 字符串查表截断碰撞。 */
static const char *
rp_unicode_as_utf8_nonul(PyObject *obj)
{
    Py_ssize_t len = 0;
    const char *s = PyUnicode_AsUTF8AndSize(obj, &len);
    if (s == NULL) {
        return NULL;
    }
    if (strlen(s) != (size_t)len) {
        PyErr_SetString(PyExc_ValueError, "embedded null character");
        return NULL;
    }
    return s;
}

#endif /* Py_RYUPY_IMPORT_REGISTRY_H */
