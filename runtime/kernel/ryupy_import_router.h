/* RyuPy 严格零字节码内核 C4-1 纯 C 导入路由器（核心加载事务分发）。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_router.h`，
 * 由 `Python/import.c` 在 `Python/ryupy_import_helpers.h` 之后包含，负责：
 * 1. `rp_load_builtin_entry`：使用 `BuiltinImporter` 类对象作为 `__loader__`，
 *    并在 `init_module_attrs` / `_PyImport_SetModule` 失败时回滚 `sys.modules`。
 * 2. `rp_load_extension_entry`：复用上游 `import_find_extension` 与
 *    `_PyImport_LoadDynamicModuleWithSpec` 加载单/多阶段 `.so` 扩展，并分离包目录。
 * 3. `rp_load_cil_entry`：执行静态 CIL 回调的单一创建/发布/初始化事务，支持独立 `package_dir`。
 * 4. `rp_kernel_import_find_and_load`：在每模块可重入锁下统一调度，子模块加载后按
 *    `parent_name` 重取 `sys.modules` 当前父包对象再挂载属性。
 */
#ifndef Py_RYUPY_IMPORT_ROUTER_H
#define Py_RYUPY_IMPORT_ROUTER_H

static PyObject *
rp_load_builtin_entry(PyThreadState *tstate, RpKernelImportState *state, PyObject *abs_name)
{
    PyObject *origin = PyUnicode_FromString("built-in");
    if (origin == NULL) return NULL;
    PyObject *spec = rp_kernel_new_module_spec(
        state, abs_name, (PyObject *)state->builtin_loader_type, origin, 0, 0, NULL);
    Py_DECREF(origin);
    if (spec == NULL) return NULL;

    PyObject *mod = create_builtin(tstate, abs_name, spec);
    if (mod == NULL) {
        Py_DECREF(spec);
        return NULL;
    }
    if (mod == Py_None) {
        Py_DECREF(mod);
        Py_DECREF(spec);
        rp_raise_not_found_fmt(tstate, abs_name, "No module named %R", abs_name, NULL);
        return NULL;
    }
    if (rp_kernel_init_module_attrs(spec, mod) < 0) {
        remove_module(tstate, abs_name);
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    ((RpModuleSpecObject *)spec)->initializing = 1;
    if (_PyImport_SetModule(abs_name, mod) < 0) {
        remove_module(tstate, abs_name);
        ((RpModuleSpecObject *)spec)->initializing = 0;
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    if (exec_builtin_or_dynamic(mod) < 0) {
        remove_module(tstate, abs_name);
        ((RpModuleSpecObject *)spec)->initializing = 0;
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    ((RpModuleSpecObject *)spec)->initializing = 0;
    if (rp_move_module_to_end(tstate, abs_name, &mod) < 0) {
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    Py_DECREF(spec);
    return mod;
}

static PyObject *
rp_load_extension_entry(PyThreadState *tstate, RpKernelImportState *state,
                        PyObject *abs_name, const RpFrozenEntry *entry,
                        PyObject *parent_spec)
{
    PyObject *origin = PyUnicode_DecodeFSDefault(entry->origin);
    if (origin == NULL) return NULL;
    PyObject *pkg_dir = (entry->package_dir != NULL && entry->package_dir[0] != '\0')
        ? PyUnicode_DecodeFSDefault(entry->package_dir)
        : Py_NewRef(Py_None);
    if (pkg_dir == NULL) {
        Py_DECREF(origin);
        return NULL;
    }
    PyObject *loader = rp_kernel_new_extension_loader(state, abs_name, origin, entry->is_package);
    if (loader == NULL) {
        Py_DECREF(pkg_dir);
        Py_DECREF(origin);
        return NULL;
    }
    PyObject *spec = rp_kernel_new_module_spec(
        state, abs_name, loader, origin, 1, entry->is_package, pkg_dir);
    Py_DECREF(loader);
    Py_DECREF(pkg_dir);
    if (spec == NULL) {
        Py_DECREF(origin);
        return NULL;
    }

    PyObject *mod = import_find_extension(tstate, abs_name, origin);
    Py_DECREF(origin);
    if (mod == NULL && _PyErr_Occurred(tstate)) {
        Py_DECREF(spec);
        return NULL;
    }
    if (mod == NULL) {
#ifdef HAVE_DYNAMIC_LOADING
        mod = _PyImport_LoadDynamicModuleWithSpec(spec, NULL);
#else
        Py_DECREF(spec);
        _PyErr_SetString(tstate, PyExc_ImportError, "dynamic loading is disabled on this build");
        return NULL;
#endif
    }
    if (mod == NULL) {
        Py_DECREF(spec);
        return NULL;
    }
    if (rp_kernel_init_module_attrs(spec, mod) < 0) {
        remove_module(tstate, abs_name);
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    ((RpModuleSpecObject *)spec)->initializing = 1;
    if (_PyImport_SetModule(abs_name, mod) < 0) {
        ((RpModuleSpecObject *)spec)->initializing = 0;
        remove_module(tstate, abs_name);
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }

    int pushed_uninit = 0;
    if (parent_spec != NULL && parent_spec != Py_None) {
        Py_ssize_t len = PyUnicode_GET_LENGTH(abs_name);
        Py_ssize_t dot = PyUnicode_FindChar(abs_name, '.', 0, len, -1);
        if (dot >= 0) {
            PyObject *child_name = PyUnicode_Substring(abs_name, dot + 1, len);
            if (child_name == NULL || rp_push_uninit_submodule(parent_spec, child_name) < 0) {
                Py_XDECREF(child_name);
                ((RpModuleSpecObject *)spec)->initializing = 0;
                remove_module(tstate, abs_name);
                Py_DECREF(mod);
                Py_DECREF(spec);
                return NULL;
            }
            Py_DECREF(child_name);
            pushed_uninit = 1;
        }
    }

    int exec_rc = exec_builtin_or_dynamic(mod);
    if (pushed_uninit) {
        rp_pop_uninit_submodule(tstate, parent_spec);
    }
    if (exec_rc < 0) {
        remove_module(tstate, abs_name);
        ((RpModuleSpecObject *)spec)->initializing = 0;
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    ((RpModuleSpecObject *)spec)->initializing = 0;
    if (rp_move_module_to_end(tstate, abs_name, &mod) < 0) {
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    Py_DECREF(spec);
    return mod;
}

static PyObject *
rp_load_cil_entry(PyThreadState *tstate, RpKernelImportState *state,
                  PyObject *abs_name, const char *abs_utf8,
                  const RpFrozenEntry *entry, PyObject *parent_spec)
{
    if (!state->has_cil_callbacks || state->cil_callbacks.exec_module == NULL) {
        _PyErr_Format(tstate, PyExc_ImportError,
                      "CIL import callback slot is not registered for module %R", abs_name);
        return NULL;
    }
    int has_loc = (entry->origin != NULL && entry->origin[0] != '\0') ? 1 : 0;
    PyObject *origin = has_loc
        ? PyUnicode_DecodeFSDefault(entry->origin)
        : PyUnicode_FromString("ryupy-cil");
    if (origin == NULL) return NULL;

    PyObject *pkg_dir = (entry->package_dir != NULL && entry->package_dir[0] != '\0')
        ? PyUnicode_DecodeFSDefault(entry->package_dir)
        : Py_NewRef(Py_None);
    if (pkg_dir == NULL) {
        Py_DECREF(origin);
        return NULL;
    }

    PyObject *loader = rp_kernel_new_cil_loader(
        state, abs_name, origin, has_loc, entry->is_package);
    if (loader == NULL) {
        Py_DECREF(pkg_dir);
        Py_DECREF(origin);
        return NULL;
    }
    PyObject *spec = rp_kernel_new_module_spec(
        state, abs_name, loader, origin, has_loc, entry->is_package, pkg_dir);
    Py_DECREF(loader);
    Py_DECREF(pkg_dir);
    Py_DECREF(origin);
    if (spec == NULL) return NULL;

    PyObject *mod = NULL;
    if (state->cil_callbacks.create_module != NULL) {
        mod = state->cil_callbacks.create_module(
            tstate->interp, abs_utf8, spec, entry->is_package, state->cil_callbacks.user_data);
        if (mod == NULL) {
            if (!_PyErr_Occurred(tstate)) {
                _PyErr_Format(tstate, PyExc_SystemError,
                              "CIL creation of module %s failed without setting an exception",
                              abs_utf8);
            }
            Py_DECREF(spec);
            return NULL;
        }
        if (_PyErr_Occurred(tstate)) {
            _PyErr_FormatFromCause(PyExc_SystemError,
                                   "CIL creation of module %s raised unreported exception",
                                   abs_utf8);
            Py_DECREF(mod);
            Py_DECREF(spec);
            return NULL;
        }
        if (mod == Py_None) {
            Py_DECREF(mod);
            mod = PyModule_NewObject(abs_name);
        }
    }
    else {
        mod = PyModule_NewObject(abs_name);
    }
    if (mod == NULL) {
        Py_DECREF(spec);
        return NULL;
    }
    if (rp_kernel_init_module_attrs(spec, mod) < 0) {
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }

    ((RpModuleSpecObject *)spec)->initializing = 1;
    if (_PyImport_SetModule(abs_name, mod) < 0) {
        ((RpModuleSpecObject *)spec)->initializing = 0;
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }

    int pushed_uninit = 0;
    if (parent_spec != NULL && parent_spec != Py_None) {
        Py_ssize_t len = PyUnicode_GET_LENGTH(abs_name);
        Py_ssize_t dot = PyUnicode_FindChar(abs_name, '.', 0, len, -1);
        if (dot >= 0) {
            PyObject *child_name = PyUnicode_Substring(abs_name, dot + 1, len);
            if (child_name == NULL || rp_push_uninit_submodule(parent_spec, child_name) < 0) {
                Py_XDECREF(child_name);
                ((RpModuleSpecObject *)spec)->initializing = 0;
                remove_module(tstate, abs_name);
                Py_DECREF(mod);
                Py_DECREF(spec);
                return NULL;
            }
            Py_DECREF(child_name);
            pushed_uninit = 1;
        }
    }

    int exec_rc = state->cil_callbacks.exec_module(
        tstate->interp, mod, spec, state->cil_callbacks.user_data);
    if (pushed_uninit) {
        rp_pop_uninit_submodule(tstate, parent_spec);
    }
    if (exec_rc != 0) {
        if (!_PyErr_Occurred(tstate)) {
            _PyErr_Format(tstate, PyExc_SystemError,
                          "CIL execution of module %s failed without setting an exception",
                          abs_utf8);
        }
        remove_module(tstate, abs_name);
        ((RpModuleSpecObject *)spec)->initializing = 0;
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    if (_PyErr_Occurred(tstate)) {
        _PyErr_FormatFromCause(PyExc_SystemError,
                               "CIL execution of module %s raised unreported exception",
                               abs_utf8);
        remove_module(tstate, abs_name);
        ((RpModuleSpecObject *)spec)->initializing = 0;
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    ((RpModuleSpecObject *)spec)->initializing = 0;
    if (rp_move_module_to_end(tstate, abs_name, &mod) < 0) {
        Py_DECREF(mod);
        Py_DECREF(spec);
        return NULL;
    }
    Py_DECREF(spec);
    return mod;
}

static PyObject *
rp_kernel_import_find_and_load(PyThreadState *tstate, PyObject *abs_name)
{
    PyInterpreterState *interp = tstate->interp;
    RpKernelImportState *state = rp_import_get_state(interp, 1);
    if (state == NULL || rp_kernel_ensure_import_types(state) < 0) {
        return NULL;
    }
    const char *abs_utf8 = rp_unicode_as_utf8_nonul(abs_name);
    if (abs_utf8 == NULL) {
        return NULL;
    }

    RpModuleLockNode *lock = NULL;
    if (rp_module_lock_acquire(state, abs_utf8, &lock) < 0) {
        return NULL;
    }

    PyObject *parent_name = NULL, *parent_spec = NULL, *child_name = NULL;
    PyObject *mod = import_get_module(tstate, abs_name);
    if (mod == NULL && _PyErr_Occurred(tstate)) {
        goto done;
    }
    if (mod == Py_None) {
        Py_CLEAR(mod);
        rp_raise_halted_none(tstate, abs_name);
        goto done;
    }
    if (mod != NULL) {
        goto done;
    }

    if (is_builtin(abs_name) != 0) {
        mod = rp_load_builtin_entry(tstate, state, abs_name);
        goto done;
    }

    if (rp_import_parent_if_needed(tstate, abs_name, &parent_name,
                                   &parent_spec, &child_name) < 0)
    {
        goto done;
    }
    mod = import_get_module(tstate, abs_name);
    if (mod == NULL && _PyErr_Occurred(tstate)) {
        goto done;
    }
    if (mod == Py_None) {
        Py_CLEAR(mod);
        rp_raise_halted_none(tstate, abs_name);
        goto done;
    }
    if (mod != NULL) {
        goto done;
    }

    const RpFrozenEntry *entry = rp_import_find_entry(state, abs_utf8);
    if (entry == NULL) {
        rp_raise_not_found_fmt(tstate, abs_name, "No module named %R", abs_name, NULL);
        goto done;
    }
    if (!state->frozen) {
        _PyErr_Format(tstate, PyExc_ImportError,
                      "RyuPy frozen import registry is not frozen yet; cannot import %R",
                      abs_name);
        goto done;
    }

    if (entry->kind == RYUPY_IMPORT_KIND_EXTENSION) {
        mod = rp_load_extension_entry(tstate, state, abs_name, entry, parent_spec);
    }
    else if (entry->kind == RYUPY_IMPORT_KIND_CIL) {
        mod = rp_load_cil_entry(tstate, state, abs_name, abs_utf8, entry, parent_spec);
    }
    else {
        _PyErr_Format(tstate, PyExc_ImportError,
                      "unknown frozen import entry kind %d for %R", entry->kind, abs_name);
    }

    if (mod != NULL && parent_name != NULL) {
        /* 对齐 _bootstrap.py:1337：子模块初始化可能替换 sys.modules[parent]，必须重取当前父模块。 */
        PyObject *cur_parent = import_get_module(tstate, parent_name);
        if (cur_parent == NULL) {
            if (!_PyErr_Occurred(tstate)) {
                _PyErr_SetObject(tstate, PyExc_KeyError, parent_name);
            }
            Py_CLEAR(mod);
        }
        else {
            if (rp_attach_to_parent(tstate, cur_parent, parent_name, child_name, mod) < 0) {
                Py_CLEAR(mod);
            }
            Py_DECREF(cur_parent);
        }
    }

done:
    Py_XDECREF(parent_name);
    Py_XDECREF(parent_spec);
    Py_XDECREF(child_name);
    rp_module_lock_release(state, lock);
    return mod;
}

#endif /* Py_RYUPY_IMPORT_ROUTER_H */
