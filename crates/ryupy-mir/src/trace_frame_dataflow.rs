//! 真实程序点/phase的保守固定点；只证明私有frame，不颁生产证书。
use crate::trace_frame_values::{Value, index};
use crate::trace_lowering::{GeneratorLayout, GeneratorOrigin};
use crate::*;
use std::collections::{HashMap, HashSet, VecDeque};
#[path = "trace_frame_dataflow_domain.rs"]
mod domain;
#[path = "trace_frame_dataflow_eval.rs"]
mod eval;
#[path = "trace_frame_dataflow_flow.rs"]
mod flow;
use domain::{Key, State, Values};
#[path = "trace_frame_dataflow_index.rs"]
mod layout_index;
use layout_index::LayoutIndex;

pub const STATE_BUDGET: usize = 32768;
pub const STEP_BUDGET: usize = 524288;
pub const VALUE_CELL_BUDGET: usize = 4000000;

#[derive(Clone, Debug, Default)]
pub struct Diagnostics {
    pub phase_keys: usize,
    pub growth_updates: usize,
    pub equal_joins: usize,
    pub work_units: usize,
    pub cumulative_content: usize,
    pub self_edges: usize,
}

#[derive(Debug)]
pub struct FrameObligations<'a> {
    pub program: &'a Program,
    pub frame_accesses: usize,
    pub exception_reads: usize,
    pub retry_edges: usize,
    pub diagnostics: Diagnostics,
    pub complete_production_certificate: bool,
    pub required_runtime_contracts: &'static [&'static str],
}

pub(super) struct Budget {
    pub diagnostics: Diagnostics,
}
impl Budget {
    fn work(&mut self, amount: usize) -> Result<(), String> {
        self.diagnostics.work_units = self.diagnostics.work_units.saturating_add(amount);
        if self.diagnostics.work_units > STEP_BUDGET {
            return Err("固定点累计工作预算耗尽".into());
        }
        Ok(())
    }
    fn charge(&mut self, state: &State) -> Result<(), String> {
        let mut cells = 0;
        for cell in &state.locals {
            self.work(1)?;
            cells += 4 * cell.len();
        }
        for cell in &state.slots {
            self.work(1)?;
            cells += 3 * cell.len();
        }
        for sources in &state.sources {
            self.work(1)?;
            cells += 2 * sources.len();
        }
        self.diagnostics.cumulative_content =
            self.diagnostics.cumulative_content.saturating_add(cells);
        if self.diagnostics.cumulative_content > VALUE_CELL_BUDGET {
            return Err("固定点累计内容预算耗尽".into());
        }
        self.diagnostics.growth_updates += 1;
        Ok(())
    }
}
struct Node {
    input: State,
    dirty: bool,
}
pub(super) struct Engine<'a> {
    body: &'a Body,
    origin: &'a GeneratorOrigin,
    layout: &'a GeneratorLayout,
    pcs: HashSet<i64>,
    lookup: LayoutIndex,
    indices: HashMap<Key, usize>,
    nodes: Vec<Node>,
    queue: VecDeque<usize>,
    budget: Budget,
    reads: usize,
    retries: usize,
}

impl Engine<'_> {
    fn enqueue(&mut self, state: State) -> Result<(), String> {
        for pc in &state.slots[0] {
            self.budget.work(1)?;
            if !matches!(pc, Value::Integer(v) if *v == -1 || self.pcs.contains(v)) {
                return Err("返回边使用未知 PC".into());
            }
            for caught in &state.slots[3] {
                self.budget.work(1)?;
                if !matches!(
                    caught,
                    Value::None | Value::Exception | Value::StoredException
                ) {
                    return Err("未知 caught 控制槽".into());
                }
                let mut incoming = state.clone();
                incoming.slots[0] = Values::single(pc.clone(), &mut self.budget)?;
                incoming.slots[3] = Values::single(caught.clone(), &mut self.budget)?;
                incoming.key.slot0 = pc.clone();
                incoming.key.slot3 = caught.clone();
                self.install(incoming)?;
            }
        }
        Ok(())
    }
    fn install(&mut self, incoming: State) -> Result<(), String> {
        self.budget.work(1)?;
        if let Some(id) = self.indices.get(&incoming.key).copied() {
            let mut merged = self.nodes[id].input.clone();
            if domain::join(&mut merged, &incoming, &mut self.budget)? {
                self.budget.charge(&merged)?;
                self.nodes[id].input = merged;
                if !self.nodes[id].dirty {
                    self.queue.push_back(id);
                }
                self.nodes[id].dirty = true;
            } else {
                self.budget.diagnostics.equal_joins += 1;
            }
        } else {
            if self.nodes.len() >= STATE_BUDGET {
                return Err("固定点phase数量预算耗尽".into());
            }
            self.budget.charge(&incoming)?;
            let id = self.nodes.len();
            self.indices.insert(incoming.key.clone(), id);
            self.nodes.push(Node {
                input: incoming,
                dirty: true,
            });
            self.queue.push_back(id);
            self.budget.diagnostics.phase_keys = self.nodes.len();
        }
        Ok(())
    }
    fn run(&mut self, slots: Vec<Value>) -> Result<(), String> {
        let state = domain::restart(
            self.body,
            self.origin,
            self.layout.initial_pc,
            slots
                .into_iter()
                .map(|value| Values::single(value, &mut self.budget))
                .collect::<Result<Vec<_>, _>>()?,
            &mut self.budget,
        )?;
        self.enqueue(state)?;
        while let Some(id) = self.queue.pop_front() {
            self.budget.work(1)?;
            let state = self.nodes[id].input.clone();
            self.nodes[id].dirty = false;
            let location = (state.key.pc, state.key.block, state.key.cursor);
            flow::step(self, state).map_err(|error| format!("{error}; position={location:?}"))?;
        }
        if self.nodes.iter().any(|node| node.dirty) {
            return Err("队列空但仍有dirty节点".into());
        }
        Ok(())
    }
}

pub fn inspect<'a>(
    program: &'a Program,
    origin: &GeneratorOrigin,
    layout: &GeneratorLayout,
) -> Result<FrameObligations<'a>, String> {
    let crate::trace_frame::Prepared {
        body,
        slots,
        frame_accesses,
        pcs,
    } = crate::trace_frame::prepare(program, origin, layout)?;
    let mut budget = Budget {
        diagnostics: Diagnostics::default(),
    };
    let lookup = LayoutIndex::build(body, layout, &mut budget)?;
    let mut engine = Engine {
        body,
        origin,
        layout,
        pcs,
        lookup,
        indices: HashMap::new(),
        nodes: Vec::new(),
        queue: VecDeque::new(),
        budget,
        reads: 0,
        retries: 0,
    };
    engine
        .run(slots)
        .map_err(|error| format!("{error}; dataflow={:?}", engine.budget.diagnostics))?;
    Ok(FrameObligations {
        program,
        frame_accesses,
        exception_reads: engine.reads,
        retry_edges: engine.retries,
        diagnostics: engine.budget.diagnostics,
        complete_production_certificate: false,
        required_runtime_contracts: &[
            "正常 next / GeneratorRetry frame协议",
            "完整operation/helper/link与EnterV1资格尚未闭合",
        ],
    })
}

#[cfg(test)]
#[path = "trace_frame_dataflow_tests.rs"]
mod tests;

#[cfg(test)]
#[path = "trace_frame_dataflow_mechanical_tests.rs"]
mod mechanical_tests;
