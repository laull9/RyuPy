//! 原生数值运算的机械CLR引用；从总引用表拆出以保持单文件职责与行数预算。
use dotnetdll::prelude::*;

pub(crate) struct MathRefs {
    pub floor_div_i64: MethodRefIndex,
    pub mod_i64: MethodRefIndex,
    pub pow_i64: MethodRefIndex,
    pub true_div_f64: MethodRefIndex,
    pub floor_div_f64: MethodRefIndex,
    pub mod_f64: MethodRefIndex,
    pub pow_f64: MethodRefIndex,
    pub f64_to_i64: MethodRefIndex,
    pub abs_i64: MethodRefIndex,
    pub abs_f64: MethodRefIndex,
    pub shl_i64: MethodRefIndex,
    pub shr_i64: MethodRefIndex,
}
impl MathRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, ryupy_runtime: AssemblyRefIndex) -> Self {
        let pymath_ref =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyMath in #ryupy_runtime });
        let pymath_type: MethodType = BaseType::class(pymath_ref).into();

        let floor_div_i64 = res.push_method_reference(
            method_ref! { static long @pymath_type::FloorDivI64(long, long) },
        );
        let mod_i64 =
            res.push_method_reference(method_ref! { static long @pymath_type::ModI64(long, long) });
        let pow_i64 =
            res.push_method_reference(method_ref! { static long @pymath_type::PowI64(long, long) });

        let true_div_f64 = res.push_method_reference(
            method_ref! { static double @pymath_type::TrueDivF64(double, double) },
        );
        let floor_div_f64 = res.push_method_reference(
            method_ref! { static double @pymath_type::FloorDivF64(double, double) },
        );
        let mod_f64 = res.push_method_reference(
            method_ref! { static double @pymath_type::ModF64(double, double) },
        );
        let pow_f64 = res.push_method_reference(
            method_ref! { static double @pymath_type::PowF64(double, double) },
        );
        let f64_to_i64 =
            res.push_method_reference(method_ref! { static long @pymath_type::F64ToI64(double) });
        let abs_i64 =
            res.push_method_reference(method_ref! { static long @pymath_type::AbsI64(long) });
        let abs_f64 =
            res.push_method_reference(method_ref! { static double @pymath_type::AbsF64(double) });
        let shl_i64 = res.push_method_reference(
            method_ref! { static long @pymath_type::ShiftLeftI64(long, long) },
        );
        let shr_i64 = res.push_method_reference(
            method_ref! { static long @pymath_type::ShiftRightI64(long, long) },
        );

        Self {
            floor_div_i64,
            mod_i64,
            pow_i64,
            true_div_f64,
            floor_div_f64,
            mod_f64,
            pow_f64,
            f64_to_i64,
            abs_i64,
            abs_f64,
            shl_i64,
            shr_i64,
        }
    }
}
