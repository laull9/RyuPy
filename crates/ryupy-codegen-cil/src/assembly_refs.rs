//! CLR 程序集身份契约：引用必须与目标框架和运行时一致，不能使用匿名零版本引用。
//!
//! C# 宿主及 Native AOT 会校验完整身份；共享框架各程序集的公钥 token 并不相同。
//! 这里只构造编译期元数据，不读取宿主反射信息，也不改变 vendor 的通用默认值。
use std::borrow::Cow;

use dotnetdll::prelude::*;

fn framework(name: &'static str, token: &'static [u8]) -> ExternalAssemblyReference<'static> {
    // 目标框架是本编译器常量，而不是未经校验的用户版本输入。
    const { assert!(crate::runtimeconfig::TARGET_FRAMEWORK_MAJOR <= u16::MAX as u32) };
    let mut reference = ExternalAssemblyReference::new(name);
    reference.version = Version {
        major: crate::runtimeconfig::TARGET_FRAMEWORK_MAJOR as u16,
        minor: 0,
        build: 0,
        revision: 0,
    };
    reference.public_key_or_token = Some(Cow::Borrowed(token));
    reference
}

pub(crate) fn system_runtime() -> ExternalAssemblyReference<'static> {
    framework(
        "System.Runtime",
        &[0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a],
    )
}

pub(crate) fn intrinsics() -> ExternalAssemblyReference<'static> {
    framework(
        "System.Runtime.Intrinsics",
        &[0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51],
    )
}

pub(crate) fn ryupy_runtime() -> ExternalAssemblyReference<'static> {
    // 独立运行时 ABI 版本，与 RyuPy.Runtime.csproj 的 AssemblyVersion 同步；不是 TFM 版本。
    let mut reference = ExternalAssemblyReference::new("RyuPy.Runtime");
    reference.version = Version {
        major: 1,
        minor: 0,
        build: 0,
        revision: 0,
    };
    reference
}
