/* RyuPy 严格零字节码内核 C4-1 每模块可重入锁、等待图死锁检测与排空实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_module_locks.h`，
 * 由 `Python/import.c` 在 `Python/ryupy_import_registry.h` 之后包含，负责：
 * 1. 每模块细粒度可重入锁（`RpModuleLockNode`）：同线程循环可重入、跨线程不同模块不互斥、
 *    跨线程竞争先释放 `table_mutex` 再释放 GIL 休眠。
 * 2. 等待图 DFS 死锁检测（`rp_has_deadlock_locked`）与空槽及时回收（`rp_blocked_pop_locked`）。
 * 3. `finalizing` 排空保护：唤醒等待线程先在无 GIL 下递减 `active_waiters` 并通知
 *    `drain_cond`，随后才重获 GIL 退出，保证持 GIL 的 `_PyImport_FiniCore` 绝不死锁或 UAF。
 * 4. 提供 `rp_kernel_ensure_initialized` 供 `import_ensure_initialized` 调用。
 */
#ifndef Py_RYUPY_MODULE_LOCKS_H
#define Py_RYUPY_MODULE_LOCKS_H

static RpBlockedThread *
rp_find_blocked_thread_locked(RpKernelImportState *state, unsigned long tid,
                              int create, size_t *out_idx)
{
    for (size_t i = 0; i < state->blocked_count; i++) {
        if (state->blocked[i].tid == tid) {
            if (out_idx != NULL) *out_idx = i;
            return &state->blocked[i];
        }
    }
    if (!create) {
        return NULL;
    }
    if (state->blocked_count == state->blocked_capacity) {
        size_t new_cap = state->blocked_capacity == 0 ? 8 : state->blocked_capacity * 2;
        RpBlockedThread *grown = (RpBlockedThread *)PyMem_RawRealloc(
            state->blocked, new_cap * sizeof(RpBlockedThread));
        if (grown == NULL) {
            return NULL;
        }
        state->blocked = grown;
        state->blocked_capacity = new_cap;
    }
    size_t idx = state->blocked_count++;
    RpBlockedThread *slot = &state->blocked[idx];
    memset(slot, 0, sizeof(*slot));
    slot->tid = tid;
    if (out_idx != NULL) *out_idx = idx;
    return slot;
}

static int
rp_blocked_push_locked(RpKernelImportState *state, unsigned long tid, RpModuleLockNode *lock)
{
    RpBlockedThread *bt = rp_find_blocked_thread_locked(state, tid, 1, NULL);
    if (bt == NULL) {
        return -1;
    }
    if (bt->count == bt->capacity) {
        size_t new_cap = bt->capacity == 0 ? 4 : bt->capacity * 2;
        RpModuleLockNode **grown = (RpModuleLockNode **)PyMem_RawRealloc(
            bt->locks, new_cap * sizeof(RpModuleLockNode *));
        if (grown == NULL) {
            return -1;
        }
        bt->locks = grown;
        bt->capacity = new_cap;
    }
    bt->locks[bt->count++] = lock;
    return 0;
}

static void
rp_blocked_pop_locked(RpKernelImportState *state, unsigned long tid, RpModuleLockNode *lock)
{
    size_t idx = 0;
    RpBlockedThread *bt = rp_find_blocked_thread_locked(state, tid, 0, &idx);
    if (bt == NULL || bt->count == 0) {
        return;
    }
    for (size_t i = bt->count; i > 0; i--) {
        if (bt->locks[i - 1] == lock) {
            for (size_t j = i - 1; j + 1 < bt->count; j++) {
                bt->locks[j] = bt->locks[j + 1];
            }
            bt->count--;
            if (bt->count == 0) {
                PyMem_RawFree(bt->locks);
                state->blocked[idx] = state->blocked[--state->blocked_count];
            }
            return;
        }
    }
}

typedef struct {
    unsigned long *items;
    size_t count;
    size_t capacity;
} RpTidVisited;

static int
rp_tid_seen(const RpTidVisited *v, unsigned long tid)
{
    for (size_t i = 0; i < v->count; i++) {
        if (v->items[i] == tid) {
            return 1;
        }
    }
    return 0;
}

static int
rp_tid_mark(RpTidVisited *v, unsigned long tid)
{
    if (v->count == v->capacity) {
        size_t new_cap = v->capacity == 0 ? 16 : v->capacity * 2;
        unsigned long *grown = (unsigned long *)PyMem_RawRealloc(
            v->items, new_cap * sizeof(unsigned long));
        if (grown == NULL) {
            return -1;
        }
        v->items = grown;
        v->capacity = new_cap;
    }
    v->items[v->count++] = tid;
    return 0;
}

static int
rp_has_deadlock_dfs_locked(RpKernelImportState *state, unsigned long target_tid,
                           unsigned long candidate_tid, RpTidVisited *visited)
{
    if (candidate_tid == PYTHREAD_INVALID_THREAD_ID) {
        return 0;
    }
    if (candidate_tid == target_tid) {
        return 1;
    }
    if (rp_tid_seen(visited, candidate_tid)) {
        return 0;
    }
    if (rp_tid_mark(visited, candidate_tid) < 0) {
        return 1;
    }
    RpBlockedThread *bt = rp_find_blocked_thread_locked(state, candidate_tid, 0, NULL);
    if (bt == NULL) {
        return 0;
    }
    for (size_t i = 0; i < bt->count; i++) {
        RpModuleLockNode *w = bt->locks[i];
        if (w->has_owner && rp_has_deadlock_dfs_locked(state, target_tid, w->owner_tid, visited)) {
            return 1;
        }
    }
    return 0;
}

static int
rp_has_deadlock_locked(RpKernelImportState *state, unsigned long target_tid,
                       unsigned long owner_tid)
{
    RpTidVisited visited = {NULL, 0, 0};
    int deadlocked = rp_has_deadlock_dfs_locked(state, target_tid, owner_tid, &visited);
    PyMem_RawFree(visited.items);
    return deadlocked;
}

static RpModuleLockNode *
rp_lock_node_acquire_ref_locked(RpKernelImportState *state, const char *name)
{
    for (RpModuleLockNode *cur = state->locks; cur != NULL; cur = cur->next) {
        if (strcmp(cur->name, name) == 0) {
            cur->refcount++;
            return cur;
        }
    }
    RpModuleLockNode *node = (RpModuleLockNode *)PyMem_RawCalloc(1, sizeof(RpModuleLockNode));
    if (node == NULL) {
        return NULL;
    }
    node->name = rp_raw_strdup(name);
    if (node->name == NULL) {
        PyMem_RawFree(node);
        return NULL;
    }
    if (PyCOND_INIT(&node->cond) != 0) {
        PyMem_RawFree(node->name);
        PyMem_RawFree(node);
        return NULL;
    }
    node->owner_tid = PYTHREAD_INVALID_THREAD_ID;
    node->refcount = 1;
    node->next = state->locks;
    state->locks = node;
    return node;
}

static void
rp_lock_node_decref_locked(RpKernelImportState *state, RpModuleLockNode *node)
{
    if (node == NULL) {
        return;
    }
    assert(node->refcount > 0);
    node->refcount--;
    if (node->refcount == 0 && node->recursion_count == 0 && node->waiters == 0) {
        RpModuleLockNode **prev = &state->locks;
        while (*prev != NULL) {
            if (*prev == node) {
                *prev = node->next;
                break;
            }
            prev = &(*prev)->next;
        }
        PyCOND_FINI(&node->cond);
        PyMem_RawFree(node->name);
        PyMem_RawFree(node);
    }
}

/* 获取指定模块名的可重入锁。返回 0 成功；-1 为系统/内存/关机错误；-2 为检测到跨线程死锁。 */
static int
rp_module_lock_acquire(RpKernelImportState *state, const char *name, RpModuleLockNode **out_lock)
{
    *out_lock = NULL;
    unsigned long me = PyThread_get_thread_ident();
    if (me == PYTHREAD_INVALID_THREAD_ID) {
        PyErr_SetString(PyExc_RuntimeError, "invalid thread id in RyuPy module lock");
        return -1;
    }

    PyMUTEX_LOCK(&state->table_mutex);
    if (state->finalizing) {
        PyMUTEX_UNLOCK(&state->table_mutex);
        PyErr_SetString(PyExc_ImportError, "interpreter is finalizing");
        return -1;
    }
    RpModuleLockNode *lock = rp_lock_node_acquire_ref_locked(state, name);
    if (lock == NULL) {
        PyMUTEX_UNLOCK(&state->table_mutex);
        PyErr_NoMemory();
        return -1;
    }
    if (!lock->has_owner || lock->owner_tid == me) {
        lock->has_owner = 1;
        lock->owner_tid = me;
        lock->recursion_count++;
        PyMUTEX_UNLOCK(&state->table_mutex);
        *out_lock = lock;
        return 0;
    }
    if (rp_blocked_push_locked(state, me, lock) < 0) {
        rp_lock_node_decref_locked(state, lock);
        PyMUTEX_UNLOCK(&state->table_mutex);
        PyErr_NoMemory();
        return -1;
    }
    if (rp_has_deadlock_locked(state, me, lock->owner_tid)) {
        rp_blocked_pop_locked(state, me, lock);
        rp_lock_node_decref_locked(state, lock);
        PyMUTEX_UNLOCK(&state->table_mutex);
        PyErr_Format(PyExc_RuntimeError,
                     "deadlock detected while importing module '%s'", name);
        return -2;
    }
    lock->waiters++;
    state->active_waiters++;
    PyMUTEX_UNLOCK(&state->table_mutex);

    /* 在释放 table_mutex 后再释放 GIL；若唤醒后发现关机，先通知 drain_cond 再重获 GIL 退出。 */
    int deadlocked = 0;
    int aborted_finalizing = 0;
    Py_BEGIN_ALLOW_THREADS
    PyMUTEX_LOCK(&state->table_mutex);
    while (!state->finalizing && lock->has_owner && lock->owner_tid != me) {
        if (rp_has_deadlock_locked(state, me, lock->owner_tid)) {
            deadlocked = 1;
            break;
        }
        PyCOND_WAIT(&lock->cond, &state->table_mutex);
    }
    if (state->finalizing) {
        aborted_finalizing = 1;
    }
    lock->waiters--;
    rp_blocked_pop_locked(state, me, lock);
    if (deadlocked || aborted_finalizing) {
        rp_lock_node_decref_locked(state, lock);
    }
    else {
        lock->has_owner = 1;
        lock->owner_tid = me;
        lock->recursion_count = 1;
    }
    state->active_waiters--;
    if (state->active_waiters == 0 && state->finalizing) {
        PyCOND_BROADCAST(&state->drain_cond);
    }
    PyMUTEX_UNLOCK(&state->table_mutex);
    Py_END_ALLOW_THREADS

    if (aborted_finalizing) {
        PyErr_SetString(PyExc_ImportError, "interpreter is finalizing");
        return -1;
    }
    if (deadlocked) {
        PyErr_Format(PyExc_RuntimeError,
                     "deadlock detected while waiting for module '%s'", name);
        return -2;
    }
    *out_lock = lock;
    return 0;
}

static void
rp_module_lock_release(RpKernelImportState *state, RpModuleLockNode *lock)
{
    if (state == NULL || lock == NULL) {
        return;
    }
    unsigned long me = PyThread_get_thread_ident();
    PyMUTEX_LOCK(&state->table_mutex);
    if (lock->has_owner && lock->owner_tid == me && lock->recursion_count > 0) {
        lock->recursion_count--;
        if (lock->recursion_count == 0) {
            lock->has_owner = 0;
            lock->owner_tid = PYTHREAD_INVALID_THREAD_ID;
            if (lock->waiters > 0) {
                PyCOND_BROADCAST(&lock->cond);
            }
        }
    }
    rp_lock_node_decref_locked(state, lock);
    PyMUTEX_UNLOCK(&state->table_mutex);
}

static int
rp_kernel_ensure_initialized(PyInterpreterState *interp, PyObject *mod, PyObject *name)
{
    (void)mod;
    RpKernelImportState *state = rp_import_get_state(interp, 0);
    if (state == NULL) {
        return 0;
    }
    const char *name_utf8 = rp_unicode_as_utf8_nonul(name);
    if (name_utf8 == NULL) {
        return -1;
    }
    RpModuleLockNode *lock = NULL;
    int rc = rp_module_lock_acquire(state, name_utf8, &lock);
    if (rc == -2) {
        PyErr_Clear();
        return 0;
    }
    if (rc < 0) {
        return -1;
    }
    rp_module_lock_release(state, lock);
    return 0;
}

#endif /* Py_RYUPY_MODULE_LOCKS_H */
