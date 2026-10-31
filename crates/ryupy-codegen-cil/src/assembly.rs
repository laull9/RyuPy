//! 组装并序列化程序集。
//!
//! 生成的程序集结构：
//!
//! ```text
//! .assembly <name>
//! .assembly extern System.Runtime
//! .assembly extern System.Console
//! .assembly extern RyuPy.Runtime
//! .class public abstract sealed RyuPy.Generated.Module      // 所有顶层函数都是它的静态方法
//!     .method public static void Main(string[] args) { .entrypoint ... }
//!     .method public static int64 fib(int64 n) { ... }
//! .class public RyuPy.Generated.<DataclassName>               // 每个 @dataclass 一个
//! ```

use ryupy_mir::Program;

use crate::{CodegenError, EmitOptions};

pub fn emit(_program: &Program, _opts: &EmitOptions) -> Result<Vec<u8>, CodegenError> {
    todo!("M1.0：用 dotnetdll 构造 Resolution 并 write，见 TODO.md")
}

pub fn dump(_program: &Program, _opts: &EmitOptions) -> Result<String, CodegenError> {
    todo!("M1.0：输出 ilasm 风格的文本，用于快照")
}
