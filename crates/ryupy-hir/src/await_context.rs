//! await 的语义来源：语言普通表达式与异步上下文协议有各自可观察的错误消息。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum AwaitContext {
    Expression,
    AsyncEnter,
    AsyncExit,
}
