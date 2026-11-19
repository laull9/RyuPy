//! Linux 专用闭包入口：将生产模块置于模块层，让函数内委托模块的测试可被测试器发现。
#![allow(dead_code)]
// 函数内模块无法注册测试，专用入口有意在模块层再编译同一生产源码。
#![allow(clippy::duplicate_mod)]

// 重用原有载荷遍历与相对路径实现；平台 ABI 是现有重定位模块的编译依赖。
#[path = "../src/cpython_platform_abi.rs"]
mod platform_abi;
#[path = "../src/cpython_platform_relocate.rs"]
mod platform_relocate;
use platform_relocate::{collect_images, relative_path};
fn rid() -> anyhow::Result<&'static str> {
    Ok("linux-x64")
}
#[path = "../src/cpython_platform_linux.rs"]
mod linux;
