/* RyuPy 严格零字节码内核 C4-1 fromlist 处理、FiniCore 安全排空与公开 ABI 实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_api_impl.h`，
 * 由 `Python/import.c` 在 `Python/ryupy_import_router.h` 之后包含，负责：
 * 1. 实现 `rp_kernel_handle_fromlist`：对齐 `_bootstrap._handle_fromlist`，支持 `*` 与
 *    `__all__`，仅忽略“缺失子模块本身”的 `ModuleNotFoundError`，内部缺失依赖与
 *    `sys.modules[sub] is None` 阻塞均原样穿透。
 * 2. 实现 `rp_kernel_import_fini_core`：在 `_PyImport_FiniCore`（`atexit` 与
 *    `finalize_modules` 之后）先立 `finalizing` 墓碑再调用 `cleanup(user_data)`、
 *    唤醒并排空活跃 `active_waiters`、最后摘除侧表槽位并释放所有锁与堆类型，防重入与 UAF。
 * 3. 实现 `Include/ryupy_import_abi.h` 声明的注册、回调设置、冻结与状态查询 C ABI。
 */
#ifndef Py_RYUPY_IMPORT_API_IMPL_H
#define Py_RYUPY_IMPORT_API_IMPL_H

/* 对齐 importlib._bootstrap._handle_fromlist：缺失子模块跳过，内部依赖缺失或 None 阻塞穿透。 */
static PyObject *
rp_kernel_handle_fromlist(PyThreadState *tstate, PyObject *mod, PyObject *fromlist,
                          PyObject *import_func, int recursive)
{
    PyObject *mod_name = PyObject_GetAttr(mod, &_Py_ID(__name__));
    if (mod_name == NULL) {
        return NULL;
    }
    PyObject *iter = PyObject_GetIter(fromlist);
    if (iter == NULL) {
        Py_DECREF(mod_name);
        return NULL;
    }

    PyObject *item = NULL;
    while ((item = PyIter_Next(iter)) != NULL) {
        if (!PyUnicode_Check(item)) {
            if (recursive) {
                _PyErr_Format(tstate, PyExc_TypeError,
                              "Item in %U.__all__ must be str, not %.200s",
                              mod_name, Py_TYPE(item)->tp_name);
            }
            else {
                _PyErr_Format(tstate, PyExc_TypeError,
                              "Item in ``from list'' must be str, not %.200s",
                              Py_TYPE(item)->tp_name);
            }
            Py_DECREF(item);
            goto error;
        }
        if (PyUnicode_CompareWithASCIIString(item, "*") == 0) {
            Py_DECREF(item);
            if (!recursive) {
                PyObject *all_attr = NULL;
                int has_all = _PyObject_LookupAttr(mod, &_Py_ID(__all__), &all_attr);
                if (has_all < 0) goto error;
                if (has_all > 0 && all_attr != NULL) {
                    PyObject *rec_res = rp_kernel_handle_fromlist(
                        tstate, mod, all_attr, import_func, 1);
                    Py_DECREF(all_attr);
                    if (rec_res == NULL) goto error;
                    Py_DECREF(rec_res);
                }
            }
            continue;
        }

        PyObject *existing_attr = NULL;
        int has_attr = _PyObject_LookupAttr(mod, item, &existing_attr);
        if (has_attr < 0) {
            Py_DECREF(item);
            goto error;
        }
        if (has_attr > 0) {
            Py_XDECREF(existing_attr);
            Py_DECREF(item);
            continue;
        }

        PyObject *from_name = PyUnicode_FromFormat("%U.%U", mod_name, item);
        Py_DECREF(item);
        if (from_name == NULL) goto error;

        PyObject *sub = import_func != NULL
            ? PyObject_CallOneArg(import_func, from_name)
            : PyImport_ImportModuleLevelObject(from_name, NULL, NULL, NULL, 0);
        if (sub != NULL) {
            Py_DECREF(sub);
            Py_DECREF(from_name);
            continue;
        }

        if (!_PyErr_ExceptionMatches(tstate, PyExc_ModuleNotFoundError)) {
            Py_DECREF(from_name);
            goto error;
        }
        PyObject *exc = _PyErr_GetRaisedException(tstate);
        PyObject *exc_name = NULL;
        int lookup_rc = _PyObject_LookupAttr(exc, &_Py_ID(name), &exc_name);
        if (lookup_rc < 0) {
            Py_DECREF(exc);
            Py_DECREF(from_name);
            goto error;
        }
        int same_name = (exc_name != NULL && PyUnicode_Check(exc_name) &&
                         PyUnicode_Compare(exc_name, from_name) == 0);
        Py_XDECREF(exc_name);

        PyObject *in_sys = import_get_module(tstate, from_name);
        Py_DECREF(from_name);
        if (in_sys == NULL && _PyErr_Occurred(tstate)) {
            Py_DECREF(exc);
            goto error;
        }
        int not_none_blocked = (in_sys != Py_None);
        Py_XDECREF(in_sys);

        if (same_name && not_none_blocked) {
            Py_DECREF(exc);
            continue;
        }
        _PyErr_SetRaisedException(tstate, exc);
        goto error;
    }
    if (_PyErr_Occurred(tstate)) {
        goto error;
    }

    Py_DECREF(iter);
    Py_DECREF(mod_name);
    return Py_NewRef(mod);

error:
    Py_DECREF(iter);
    Py_DECREF(mod_name);
    return NULL;
}

/* 在 _PyImport_FiniCore（atexit 与 finalize_modules 之后）清理全部注册表、回调与锁。 */
static void
rp_kernel_import_fini_core(PyInterpreterState *interp)
{
    if (interp == NULL || !rp_side_table_mutex_ready) {
        return;
    }
    int64_t interp_id = interp->id;
    RpKernelImportState *state = NULL;

    /* 1. 在短互斥锁下置 finalizing 墓碑，防止 cleanup 回调重入在侧表重建游离状态。 */
    PyMUTEX_LOCK(&rp_side_table_mutex);
    RpSideTableSlot *slot = rp_side_table_find_locked(interp, interp_id);
    if (slot == NULL || slot->finalizing) {
        PyMUTEX_UNLOCK(&rp_side_table_mutex);
        return;
    }
    slot->finalizing = 1;
    state = slot->state;
    PyMUTEX_UNLOCK(&rp_side_table_mutex);

    if (state != NULL) {
        /* 2. 在锁外先后调用 CIL cleanup 与独立 FiniCore Fence 钩子。 */
        if (state->has_cil_callbacks && state->cil_callbacks.cleanup != NULL) {
            rp_cil_callback_cleanup_fn fn = state->cil_callbacks.cleanup;
            void *ud = state->cil_callbacks.user_data;
            state->has_cil_callbacks = 0;
            memset(&state->cil_callbacks, 0, sizeof(state->cil_callbacks));
            fn(ud);
        }
        if (state->fini_fence_fn != NULL) {
            rp_kernel_fini_fence_fn fence_fn = state->fini_fence_fn;
            void *fence_ud = state->fini_fence_user_data;
            state->fini_fence_fn = NULL;
            state->fini_fence_user_data = NULL;
            fence_fn(fence_ud);
        }

        /* 3. 置位 state->finalizing、广播所有等待节点并排空 active_waiters，消除 UAF。 */
        if (state->sync_initialized) {
            PyMUTEX_LOCK(&state->table_mutex);
            state->finalizing = 1;
            for (RpModuleLockNode *n = state->locks; n != NULL; n = n->next) {
                PyCOND_BROADCAST(&n->cond);
            }
            while (state->active_waiters > 0) {
                PyCOND_WAIT(&state->drain_cond, &state->table_mutex);
            }
            PyMUTEX_UNLOCK(&state->table_mutex);
        }

        /* 4. 保留侧表 slot->finalizing 墓碑直至全部 PyObject 类型引用与 state 彻底清完，
         * 防止 Py_CLEAR 析构期或晚期 GC 重入 import_find_and_load 时重建游离 state。 */
        for (size_t i = 0; i < state->entry_count; i++) {
            PyMem_RawFree(state->entries[i].name);
            PyMem_RawFree(state->entries[i].origin);
            PyMem_RawFree(state->entries[i].package_dir);
        }
        PyMem_RawFree(state->entries);

        while (state->locks != NULL) {
            RpModuleLockNode *next = state->locks->next;
            PyCOND_FINI(&state->locks->cond);
            PyMem_RawFree(state->locks->name);
            PyMem_RawFree(state->locks);
            state->locks = next;
        }
        for (size_t i = 0; i < state->blocked_count; i++) {
            PyMem_RawFree(state->blocked[i].locks);
        }
        PyMem_RawFree(state->blocked);
        if (state->sync_initialized) {
            PyCOND_FINI(&state->drain_cond);
            PyMUTEX_FINI(&state->table_mutex);
        }

        Py_CLEAR(state->spec_type);
        Py_CLEAR(state->builtin_loader_type);
        Py_CLEAR(state->extension_loader_type);
        Py_CLEAR(state->cil_loader_type);
        PyMem_RawFree(state);
    }

    /* 5. 全部 waiter 排空且 state 所拥有的 PyObject 均已清空后，再摘除侧表墓碑槽位。 */
    PyMUTEX_LOCK(&rp_side_table_mutex);
    RpSideTableSlot **prev = &rp_side_table_head;
    while (*prev != NULL) {
        if (*prev == slot) {
            *prev = slot->next;
            break;
        }
        prev = &(*prev)->next;
    }
    PyMUTEX_UNLOCK(&rp_side_table_mutex);
    PyMem_RawFree(slot);
}

/* --- 公开 C ABI 实现（仅支持已初始化解释器及所属 GIL） ---------------- */

int
rp_kernel_import_register(PyInterpreterState *interp, const RpKernelImportEntry *entry)
{
    if (rp_verify_interp_ownership(interp, NULL) < 0) {
        return -1;
    }
    if (entry == NULL) {
        PyErr_SetString(PyExc_ValueError, "import entry must not be NULL");
        return -1;
    }
    if (entry->abi_version != RYUPY_IMPORT_ABI_VERSION ||
        entry->struct_size != sizeof(RpKernelImportEntry))
    {
        PyErr_Format(PyExc_ValueError,
                     "bad RyuPy import entry ABI (version=%u size=%u)",
                     entry->abi_version, entry->struct_size);
        return -1;
    }
    if (entry->kind != RYUPY_IMPORT_KIND_EXTENSION && entry->kind != RYUPY_IMPORT_KIND_CIL) {
        PyErr_Format(PyExc_ValueError, "unsupported RyuPy import entry kind %d", entry->kind);
        return -1;
    }
    if (rp_validate_module_name(entry->name) < 0) {
        PyErr_SetString(PyExc_ValueError, "invalid module name for frozen import entry");
        return -1;
    }
    if (entry->kind == RYUPY_IMPORT_KIND_EXTENSION &&
        (entry->origin == NULL || entry->origin[0] == '\0'))
    {
        PyErr_Format(PyExc_ValueError,
                     "extension entry '%s' requires explicit origin path", entry->name);
        return -1;
    }
    if (!entry->is_package && entry->package_dir != NULL && entry->package_dir[0] != '\0') {
        PyErr_Format(PyExc_ValueError,
                     "non-package entry '%s' must not specify package_dir", entry->name);
        return -1;
    }
    if ((entry->origin != NULL && !rp_is_valid_utf8_cstr(entry->origin)) ||
        (entry->package_dir != NULL && !rp_is_valid_utf8_cstr(entry->package_dir)))
    {
        PyErr_SetString(PyExc_ValueError, "origin/package_dir must be valid UTF-8");
        return -1;
    }

    RpKernelImportState *state = rp_import_get_state(interp, 1);
    if (state == NULL) {
        if (!PyErr_Occurred()) {
            PyErr_SetString(PyExc_RuntimeError, "import registry is unavailable or finalizing");
        }
        return -1;
    }
    if (state->frozen) {
        PyErr_Format(PyExc_RuntimeError,
                     "cannot register module '%s': import registry is already frozen",
                     entry->name);
        return -1;
    }
    if (state->cil_cleanup_in_progress) {
        PyErr_SetString(PyExc_RuntimeError,
                        "cannot register module during CIL callback cleanup transaction");
        return -1;
    }
    if (rp_import_find_entry(state, entry->name) != NULL) {
        PyErr_Format(PyExc_ValueError,
                     "duplicate frozen import registration for module '%s'", entry->name);
        return -1;
    }

    if (state->entry_count == state->entry_capacity) {
        size_t new_cap = state->entry_capacity == 0 ? 16 : state->entry_capacity * 2;
        RpFrozenEntry *grown = (RpFrozenEntry *)PyMem_RawRealloc(
            state->entries, new_cap * sizeof(RpFrozenEntry));
        if (grown == NULL) {
            PyErr_NoMemory();
            return -1;
        }
        state->entries = grown;
        state->entry_capacity = new_cap;
    }

    char *name_copy = rp_raw_strdup(entry->name);
    char *origin_copy = (entry->origin && entry->origin[0] != '\0')
        ? rp_raw_strdup(entry->origin)
        : NULL;
    char *pkg_dir_copy = (entry->package_dir && entry->package_dir[0] != '\0')
        ? rp_raw_strdup(entry->package_dir)
        : NULL;
    if (name_copy == NULL ||
        (entry->origin != NULL && entry->origin[0] != '\0' && origin_copy == NULL) ||
        (entry->package_dir != NULL && entry->package_dir[0] != '\0' && pkg_dir_copy == NULL))
    {
        PyMem_RawFree(name_copy);
        PyMem_RawFree(origin_copy);
        PyMem_RawFree(pkg_dir_copy);
        PyErr_NoMemory();
        return -1;
    }
    RpFrozenEntry *slot = &state->entries[state->entry_count++];
    slot->kind = entry->kind;
    slot->is_package = entry->is_package ? 1 : 0;
    slot->name = name_copy;
    slot->origin = origin_copy;
    slot->package_dir = pkg_dir_copy;
    return 0;
}

int
rp_kernel_import_set_cil_callbacks(PyInterpreterState *interp,
                                   const RpKernelCilImportCallbacks *callbacks)
{
    if (rp_verify_interp_ownership(interp, NULL) < 0) {
        return -1;
    }
    if (callbacks == NULL) {
        PyErr_SetString(PyExc_ValueError, "CIL callbacks struct must not be NULL");
        return -1;
    }
    if (callbacks->abi_version != RYUPY_IMPORT_ABI_VERSION ||
        callbacks->struct_size != sizeof(RpKernelCilImportCallbacks))
    {
        PyErr_Format(PyExc_ValueError,
                     "bad RyuPy CIL callback ABI (version=%u size=%u)",
                     callbacks->abi_version, callbacks->struct_size);
        return -1;
    }
    if (callbacks->exec_module == NULL) {
        PyErr_SetString(PyExc_ValueError, "CIL exec_module callback must not be NULL");
        return -1;
    }
    RpKernelImportState *state = rp_import_get_state(interp, 1);
    if (state == NULL) {
        if (!PyErr_Occurred()) {
            PyErr_SetString(PyExc_RuntimeError, "import registry is unavailable or finalizing");
        }
        return -1;
    }
    if (state->frozen) {
        PyErr_SetString(PyExc_RuntimeError,
                        "cannot replace CIL import callbacks after registry freeze");
        return -1;
    }
    if (state->cil_cleanup_in_progress) {
        PyErr_SetString(PyExc_RuntimeError,
                        "re-entrant rp_kernel_import_set_cil_callbacks during cleanup is not allowed");
        return -1;
    }
    if (PyErr_Occurred() != NULL) {
        return -1;
    }
    if (state->has_cil_callbacks && state->cil_callbacks.cleanup != NULL) {
        rp_cil_callback_cleanup_fn old_cleanup = state->cil_callbacks.cleanup;
        void *old_ud = state->cil_callbacks.user_data;
        state->has_cil_callbacks = 0;
        memset(&state->cil_callbacks, 0, sizeof(state->cil_callbacks));
        /* 开启回调配置事务：运行旧 cleanup 时阻断重入 freeze / replace / register，
         * 防止绕过冻结或覆盖新配置；若旧 cleanup 留下异常则返回 -1 且不接管新 callbacks 所有权。 */
        state->cil_cleanup_in_progress = 1;
        old_cleanup(old_ud);
        state->cil_cleanup_in_progress = 0;
        if (PyErr_Occurred() != NULL) {
            return -1;
        }
    }
    state->cil_callbacks = *callbacks;
    state->has_cil_callbacks = 1;
    return 0;
}

int
rp_kernel_set_fini_fence(PyInterpreterState *interp,
                         rp_kernel_fini_fence_fn fence_fn,
                         void *user_data)
{
    if (rp_verify_interp_ownership(interp, NULL) < 0) {
        return -1;
    }
    if (fence_fn == NULL) {
        PyErr_SetString(PyExc_ValueError, "FiniCore fence callback must not be NULL");
        return -1;
    }
    RpKernelImportState *state = rp_import_get_state(interp, 1);
    if (state == NULL) {
        if (!PyErr_Occurred()) {
            PyErr_SetString(PyExc_RuntimeError, "import registry is unavailable or finalizing");
        }
        return -1;
    }
    if (state->cil_cleanup_in_progress) {
        PyErr_SetString(PyExc_RuntimeError,
                        "cannot update FiniCore fence during CIL callback cleanup transaction");
        return -1;
    }
    if (state->fini_fence_fn != NULL) {
        if (state->fini_fence_fn == fence_fn && state->fini_fence_user_data == user_data) {
            return 0;
        }
        PyErr_SetString(PyExc_RuntimeError,
                        "FiniCore fence is write-once per interpreter and cannot be cleared or replaced");
        return -1;
    }
    state->fini_fence_fn = fence_fn;
    state->fini_fence_user_data = user_data;
    return 0;
}

int
rp_kernel_import_freeze(PyInterpreterState *interp)
{
    if (rp_verify_interp_ownership(interp, NULL) < 0) {
        return -1;
    }
    RpKernelImportState *state = rp_import_get_state(interp, 1);
    if (state == NULL) {
        if (!PyErr_Occurred()) {
            PyErr_SetString(PyExc_RuntimeError, "import registry is unavailable or finalizing");
        }
        return -1;
    }
    if (state->cil_cleanup_in_progress) {
        PyErr_SetString(PyExc_RuntimeError,
                        "cannot freeze import registry during CIL callback cleanup transaction");
        return -1;
    }
    state->frozen = 1;
    return 0;
}

int
rp_kernel_import_is_frozen(PyInterpreterState *interp)
{
    if (rp_verify_interp_ownership(interp, NULL) < 0) {
        return -1;
    }
    RpKernelImportState *state = rp_import_get_state(interp, 0);
    return (state != NULL && state->frozen) ? 1 : 0;
}

Py_ssize_t
rp_kernel_import_entry_count(PyInterpreterState *interp)
{
    if (rp_verify_interp_ownership(interp, NULL) < 0) {
        return -1;
    }
    RpKernelImportState *state = rp_import_get_state(interp, 0);
    return state != NULL ? (Py_ssize_t)state->entry_count : 0;
}

#endif /* Py_RYUPY_IMPORT_API_IMPL_H */
