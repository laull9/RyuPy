"""有限IR门禁的纯结构正负例；不运行编译器或被测程序。"""
import unittest
import module_call_ir as ir


def matmul_text(candidate=True):
    fallback = '_7 = RuntimeFn::IsModule(_1);\n        _9 = RuntimeFn::CaptureModuleTarget(_1, const "append");\n        _8 = dyn.captured_method(_7, _9, "append", [_2]);' if candidate else '_8 = dyn.method(_1, "append", [_2]);'
    mir='''fn matmul(_0: dyn) -> dyn {
    bb0: {
        _1 = RuntimeFn::NumericListCreate();
        _2 = RuntimeFn::NumericListReadCheckedF64(_0, _1);
        _3 = RuntimeFn::NumericListReadCheckedF64(_0, _1);
    }
    bb1: {
        _4 = RuntimeFn::NumericListAppendF64(_1, _2);
    }
    bb2: {
        '''+fallback+'''
    }
}
'''
    cil='''    .method public static object m0_matmul(object) cil managed
    {
        IL_0000: Call(static object X.PyNumericListOps.Create())
        IL_0001: Call(static double X.PyNumericListOps.ReadCheckedF64(object, object))
        IL_0002: Call(static double X.PyNumericListOps.ReadCheckedF64(object, object))
        IL_0003: Branch(4)
        IL_0004: Call(static object X.PyNumericListOps.AppendF64(object, double))
        IL_0005: Return
    }
'''
    return mir,cil


def math_text(candidate=True):
    if candidate:
        body='        _5 = RuntimeFn::IsModule(_1);\n        _3 = RuntimeFn::CaptureModuleTarget(_1, const "sqrt");\n        _2 = box<F64>(const 4_f64);\n        _4 = dyn.captured_method(_5, _3, "sqrt", [_2]);'
        ops=['LoadLocal(1)','Call(static bool X.PyOps.IsModule(object))','StoreLocal(5)',
             'LoadLocal(1)','LoadString("sqrt")','Call(static object X.PyOps.CaptureModuleTarget(object, string))','StoreLocal(3)',
             'LoadStaticField(literal)','StoreLocal(2)','LoadConstantInt32(0)','LoadLocal(5)','LoadLocal(3)',
             'LoadString("sqrt")','LoadLocal(2)','Call(static object X.PyPic.CallCapturedMethod1(int, bool, object, string, object))']
    else:
        body='        _4 = dyn.method(_1, "sqrt", [_2]);'
        ops=['Call(static object X.PyPic.CallMethod1(int, object, string, object))']
    mir='fn run(_0: dyn) -> dyn {\n    bb0: {\n        // 捕获前已有接收者求值\n'+body+'\n    }\n}\n'
    cil='    .method public static object m0_run(object) cil managed\n    {\n'+''.join(f'        IL_{i:04x}: {op}\n' for i,op in enumerate(ops))+'    }\n'
    return mir,cil


class IrTests(unittest.TestCase):
    def test_numeric_paths_and_counts_are_required(self):
        for candidate in (False,True):
            m,c=matmul_text(candidate);self.assertEqual(ir.matmul(m,c,candidate)['read']['mir'],2)
        with self.assertRaises(ValueError):ir.matmul(m.replace('NumericListReadCheckedF64','DynRead',1),c,True)
        with self.assertRaises(ValueError):ir.matmul(m,c.replace('.PyNumericListOps.AppendF64(','.Wrong.AppendF64('),True)

    def test_typed_capture_and_lost_fallback_fail(self):
        m,c=matmul_text()
        with self.assertRaises(ValueError):ir.matmul(m.replace('_4 = RuntimeFn::NumericListAppendF64','_7 = RuntimeFn::CaptureModuleMethod(_1, const "append");\n        _4 = RuntimeFn::NumericListAppendF64'),c,True)
        with self.assertRaises(ValueError):ir.matmul(m,c.replace('IL_0004:', 'IL_0004: Call(static object X.PyOps.CaptureModuleMethod(object, string))\n        IL_0006:'),True)
        with self.assertRaises(ValueError):ir.matmul(m.replace('CaptureModuleTarget','Other'),c,True)

    def test_math_pair_receiver_flag_target_and_baseline(self):
        for candidate in (False,True):ir.math_module(*math_text(candidate),candidate)
        m,c=math_text()
        for bad in [m.replace('IsModule(_1)','IsModule(_9)'),
                    m.replace('dyn.captured_method(_5','dyn.captured_method(_6'),
                    m.replace('(_5, _3, "sqrt"','(_5, _1, "sqrt"')]:
            with self.assertRaises(ValueError):ir.math_module(bad,c,True)
        with self.assertRaises(ValueError):ir.math_module(m,c,False)
        with self.assertRaises(ValueError):ir.math_module(m,c.replace('CaptureModuleTarget','Other'),True)

    def test_pair_and_argument_order_fail(self):
        m,c=math_text();lines=m.splitlines()
        for a,b in [(3,4),(4,5)]:
            bad=lines.copy();bad[a],bad[b]=bad[b],bad[a]
            with self.assertRaises(ValueError):ir.math_module('\n'.join(bad),c,True)
        with self.assertRaises(ValueError):ir.math_module(m,c.replace('LoadLocal(5)','LoadLocal(9)'),True)
        with self.assertRaises(ValueError):ir.math_module(m,c.replace('LoadLocal(3)','LoadLocal(1)'),True)

    def test_pair_local_rewrites_fail(self):
        m,c=math_text()
        for marker,write in [('        _3 =','        _1 = _9;\n'),
                             ('        _2 =','        _5 = const false;\n'),
                             ('        _2 =','        _3 = _9;\n')]:
            with self.assertRaises(ValueError):ir.math_module(m.replace(marker,write+marker),c,True)
        for marker,slot in [('        IL_0003:',1),('        IL_0007:',5),('        IL_0007:',3)]:
            with self.assertRaises(ValueError):ir.math_module(m,c.replace(marker,f'        IL_ffff: StoreLocal({slot})\n'+marker),True)

    def test_typed_block_rejects_both_pair_parts(self):
        m,c=matmul_text()
        for helper in ('IsModule','CaptureModuleTarget'):
            with self.assertRaises(ValueError):ir.matmul(m.replace('_4 = RuntimeFn::NumericListAppendF64',f'_7 = RuntimeFn::{helper}(_1);\n        _4 = RuntimeFn::NumericListAppendF64'),c,True)


if __name__=='__main__':unittest.main()
