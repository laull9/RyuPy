"""有限IR门禁的纯结构正负例；不运行编译器或被测程序。"""
import unittest
import module_call_ir as ir


def matmul_text(candidate=True):
    fallback = '_9 = RuntimeFn::CaptureModuleMethod(_1, const "append");\n        _8 = dyn.method(_9, "append", [_2]);' if candidate else '_8 = dyn.method(_1, "append", [_2]);'
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
    cap='_3 = RuntimeFn::CaptureModuleMethod(_1, const "sqrt");\n' if candidate else ''
    receiver='_3' if candidate else '_1'
    mir='fn run(_0: dyn) -> dyn {\n'+cap+'_4 = dyn.method('+receiver+', "sqrt", [_2]);\n}\n'
    ops='        IL_0000: Call(static object X.PyOps.CaptureModuleMethod(object, string))\n' if candidate else ''
    cil='    .method public static object m0_run(object) cil managed\n    {\n'+ops+'        IL_0001: Call(static object X.PyPic.CallMethod1(int, object, string, object))\n    }\n'
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
        with self.assertRaises(ValueError):ir.matmul(m.replace('CaptureModuleMethod','Other'),c,True)

    def test_math_token_receiver_order_and_baseline(self):
        for candidate in (False,True):ir.math_module(*math_text(candidate),candidate)
        m,c=math_text()
        with self.assertRaises(ValueError):ir.math_module(m.replace('dyn.method(_3','dyn.method(_1'),c,True)
        with self.assertRaises(ValueError):ir.math_module(m,c,False)
        with self.assertRaises(ValueError):ir.math_module(m,c.replace('CaptureModuleMethod','Other'),True)


if __name__=='__main__':unittest.main()
