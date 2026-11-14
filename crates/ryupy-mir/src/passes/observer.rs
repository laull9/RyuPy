//! 流水线诊断通知：只输出固定元数据，不暴露可变IR或控制调度。
//! 观察接口不依赖driver、时钟或I/O；默认观察者不求值诊断名称。

#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub enum PipelinePhase {
    Run,
    Validate,
}

#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub enum PipelineStep {
    Begin,
    End,
}

#[derive(Clone, Copy, Debug, Eq, PartialEq)]
pub struct PipelineEvent {
    /// 从1开始，区分同一流水线中重复出现的同名pass。
    pub ordinal: usize,
    pub pass_name: &'static str,
    pub phase: PipelinePhase,
    pub step: PipelineStep,
}

/// 仅通知，不决定pass执行或校验结果；实现者应避免panic。
pub trait PipelineObserver {
    const ENABLED: bool = true;
    fn observe(&mut self, event: PipelineEvent);
}

pub(super) struct NoopObserver;
impl PipelineObserver for NoopObserver {
    const ENABLED: bool = false;
    fn observe(&mut self, _: PipelineEvent) {}
}
