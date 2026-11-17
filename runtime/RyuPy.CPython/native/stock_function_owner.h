/* 薄函数的 GC 可见 owner：F -> closure/cell -> P -> owner -> F。
 * P 的 self 为本对象，纯 native context 仅存 core token 与登记链，不持 Python 引用。
 * code 不得携带实例 P：常规 GIL 的 stock code 对象无 GC traverse，会造成环不可回收。
 * 生命周期完全依赖公开强引用/GC API，不使用 weak callback、watcher 或 raw-F 见证。
 * P 单独外逃时持有 F；最终释放外部根后 CP GC 收集环，core 无 owned CP 回边。
 */
#ifndef RYUPY_STOCK_FUNCTION_OWNER_H
#define RYUPY_STOCK_FUNCTION_OWNER_H
#include <Python.h>

typedef void (*rp_fo_action)(void *);
typedef struct {
    PyObject_HEAD
    PyObject *function;
    void *context;
    rp_fo_action detach, dispose;
} RpFunctionOwner;
static PyObject *rp_fo_type;

static int rp_fo_traverse(RpFunctionOwner *self, visitproc visit, void *arg) {
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->function);
    return 0;
}
static int rp_fo_clear(RpFunctionOwner *self) {
    /* tp_clear 可多次调用；置 NULL 先于 DECREF，重入者只能看到已撤销的环境。 */
    Py_CLEAR(self->function);
    return 0;
}
static void rp_fo_dealloc(RpFunctionOwner *self) {
    PyTypeObject *type = Py_TYPE(self);
    PyObject_GC_UnTrack(self);
    /* native 登记链必须先摘除；不借助可被 gc.get_referents 独立保活的 capsule。 */
    if (self->detach) self->detach(self->context);
    (void)rp_fo_clear(self);
    if (self->dispose) self->dispose(self->context);
    type->tp_free((PyObject *)self);
    Py_DECREF(type);
}
static int rp_fo_start(void) {
    if (rp_fo_type) return 0;
    PyType_Slot slots[] = {
        {Py_tp_traverse, rp_fo_traverse}, {Py_tp_clear, rp_fo_clear},
        {Py_tp_dealloc, rp_fo_dealloc}, {0, NULL}
    };
    PyType_Spec spec = {"ryupy.FunctionOwner", sizeof(RpFunctionOwner), 0,
                       Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC, slots};
    rp_fo_type = PyType_FromSpec(&spec);
    return rp_fo_type ? 0 : -1;
}
static RpFunctionOwner *rp_fo_new(void *context, rp_fo_action detach, rp_fo_action dispose) {
    if (rp_fo_start() < 0) return NULL;
    /* GenericAlloc 已清零/跟踪实例；NULL function 是有效的 traverse 状态。 */
    RpFunctionOwner *owner = (RpFunctionOwner *)PyType_GenericAlloc((PyTypeObject *)rp_fo_type, 0);
    if (!owner) return NULL;
    owner->context = context; owner->detach = detach; owner->dispose = dispose;
    return owner;
}
static void rp_fo_bind(RpFunctionOwner *owner, PyObject *function) {
    /* 仅创建阶段绑定一次；无分配或 Python 回调。 */
    owner->function = Py_NewRef(function);
}
static void rp_fo_unbind(RpFunctionOwner *owner) { Py_CLEAR(owner->function); }
static PyObject *rp_fo_borrow(RpFunctionOwner *owner) {
    return owner && owner->function ? owner->function : Py_None;
}
static void *rp_fo_context(PyObject *self) {
    RpFunctionOwner *owner = (RpFunctionOwner *)self;
    if (!owner->function) { PyErr_SetString(PyExc_RuntimeError, "薄函数环境已清理"); return NULL; }
    return owner->context;
}
static void rp_fo_stop(void) { Py_CLEAR(rp_fo_type); }
#endif
