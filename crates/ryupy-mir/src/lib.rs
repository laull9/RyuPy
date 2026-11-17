//! MIR（Mid-level IR）：基于基本块的控制流图。
//!
//! 设计取舍（见 ADR-0002）：
//! - Phase 1 采用 **rustc 风格的「局部变量 + 基本块」非 SSA 形式**，而不是严格 SSA。
//!   CIL 本身就是「局部变量槽 + 求值栈」，非 SSA 形式可以几乎一一映射，省掉 φ 节点消解。
//!   Phase 3 做数据流优化时，再按需在 MIR 之上构造 SSA 视图。
//! - MIR 的类型已经「定型」：每个局部变量都有确定的 [`MirType`]，操作已区分整数 / 浮点版本，
//!   Python 特有语义（`//`、`%`、`**`）已改写为运行时调用。codegen 不需要再理解 Python。
//! - MIR 有自己的 ID 体系（[`FnId`]），**codegen 只依赖本 crate，不接触 HIR**。

pub mod build;
mod build_assign;
mod build_class;
mod build_context;
mod class_cells;
mod class_cleanup;
mod core_types;
#[cfg(test)]
mod generic_core_tests;
mod generic_cores;
mod validate_generic_cores;
pub use class_cells::{ClassCellCapture, ClassCellDefinition, ClassCellEnvSource};
mod lexical_super;
#[cfg(test)]
mod lexical_super_tests;
pub use lexical_super::{SuperClassSource, ZeroCallFrame};
mod build_decorators;
mod build_dynamic_try;
mod build_expr_context;
mod build_flow;
mod build_generic_range;
mod build_loops;
mod build_params;
mod build_print;
mod build_product_trace;
mod build_relative_import;
mod build_stmts;
mod build_try;
mod call_builder;
mod call_builtins;
mod call_defaults;
mod call_function_origin;
mod closure_env;
mod compare_chain;
mod dynamic_origin_build;
mod exact_types;
pub mod expr_builder;
mod expr_ops;
mod expr_values;
#[cfg(test)]
mod function_defaults_tests;
mod function_locals;
mod function_module;
#[cfg(test)]
mod function_module_tests;
mod generator_exceptions;
pub mod generator_product;
mod generator_product_cursor;
mod generator_product_defaults;
#[cfg(test)]
mod generator_product_defaults_tests;
mod generator_product_edge_witness;
mod generator_product_edges;
mod generator_product_effects;
mod generator_product_failure;
mod generator_product_high;
mod generator_product_identity;
mod generator_product_initialization;
#[cfg(test)]
mod generator_product_initialization_tests;
mod generator_product_link;
mod generator_product_routes;
#[cfg(test)]
mod generator_product_selection_tests;
mod generator_product_snapshot;
mod generator_product_structure;
#[cfg(test)]
mod generator_product_structure_tests;
#[cfg(test)]
mod generator_product_tests;
mod generator_product_trace;
mod generator_product_values;
pub(crate) mod generator_readonly_params;
#[cfg(test)]
mod generator_readonly_params_tests;
mod generators;

mod async_iteration;
mod async_iteration_abi;
#[cfg(all(test, feature = "native-coroutines"))]
mod async_iteration_tests;
mod fixed_runtime_abi;
pub mod link;
mod link_placeholders;
mod loop_assignment_join;
mod mir;
mod native_modules;
pub use native_modules::NativeModuleRequirements;
mod module_execution;
mod module_location;
pub mod native_abi;
mod operations;
mod trace_product_abi;
mod traceback_protocol;
mod validate_lexical_super;
mod validate_regions;
pub use module_execution::{
    GlobalStorage, InstanceStateSource, LegacySourceEntry, ModuleExecutionKind,
};
pub mod inspect_instance_contract;
#[cfg(test)]
mod inspect_instance_tests;
mod instance_module;
pub mod source_provenance;
#[cfg(test)]
mod source_provenance_tests;
mod validate_class_cell_uses;
mod validate_class_cells;
mod validate_instance_module;
pub use module_location::ModuleLocation;
pub mod passes;
pub mod pretty;
mod runtime_fn;
pub mod validate;

pub use build::build_program;
pub use link::{UnitMeta, link};
pub use mir::*;

#[cfg(test)]
mod generator_dynamic_tests;
#[cfg(test)]
mod generator_tests;

mod yield_expr;

mod prepared_call;

mod container_literals;
mod dict_unpack;

#[cfg(test)]
mod print_tests;

mod module_bindings;
mod module_delete;
mod module_names;

#[cfg(test)]
mod binding_validation_tests;
mod validate_exports;

mod class_lookup;

mod validate_dynamic_origin;
mod validate_identities;

#[cfg(test)]
mod numeric_list_abi_tests;

mod validate_vectors;
#[cfg(test)]
mod vector_tests;

#[cfg(test)]
mod list_read_tests;
mod validate_list_reads;

#[cfg(test)]
mod module_location_tests;
mod module_metadata;

#[cfg(test)]
mod module_capture_tests;

mod validate_module_pairs;

#[cfg(test)]
mod module_pair_tests;

#[cfg(test)]
mod ordinary_class_finalize_tests;

#[cfg(test)]
mod unpack_ex_abi_tests;

#[cfg(test)]
mod dynamic_origin_tests;

#[cfg(test)]
mod function_locals_tests;

mod function_definition;
pub use function_definition::{DefinitionInfo, PythonFunctionKind};
#[cfg(test)]
mod function_definition_tests;

#[cfg(test)]
mod instance_module_tests;

#[cfg(test)]
mod module_delete_tests;

#[cfg(test)]
mod module_delete_abi_tests;

#[cfg(test)]
mod builtin_lookup_tests;

#[cfg(test)]
mod syntax_error_tests;

#[cfg(test)]
mod class_cell_tests;

#[cfg(test)]
mod relative_import_tests;

mod source_handler_contract;
pub use source_handler_contract::SourceHandlerContract;
#[cfg(test)]
mod instance_handler_tests;

#[cfg(feature = "trace-adapter-test")]
mod trace_adapter_test;
#[cfg(all(test, feature = "trace-adapter-test"))]
mod trace_adapter_test_tests;

pub mod trace_domain;
mod trace_domain_check;
#[cfg(test)]
mod trace_domain_tests;
mod trace_domain_transfer;

pub mod trace_final_audit;
pub mod trace_lowering;
#[cfg(test)]
mod trace_lowering_tests;

pub mod trace_frame;
pub mod trace_frame_dataflow;
mod trace_frame_shape;
#[cfg(test)]
mod trace_frame_tests;
mod trace_frame_values;

mod lexical_class_abi;

mod build_lexical_class;

mod lexical_cell_read;
mod lexical_class_cells;
pub use lexical_class_cells::{LexicalClassCell, LexicalClassPublication};

mod lexical_class_publish;

#[cfg(test)]
mod lexical_class_tests;

#[cfg(test)]
mod direct_globals_tests;

mod direct_globals_contract;
pub use direct_globals_contract::DirectGlobalsContract;
#[cfg(test)]
mod direct_globals_contract_tests;

#[cfg(test)]
mod ordinary_mi_tests;

mod private_comp_append;
#[cfg(test)]
mod private_comp_append_tests;
mod private_comp_capacity;

mod generator_state;
mod generator_state_analysis;
mod generator_state_validate;
mod link_values;
mod local_access;

#[cfg(test)]
mod generator_state_tests;
// 批量sum准入仍处于独审/CI原型阶段，生产构建不包含该计划或启用开关。
#[cfg(feature = "generator-sum-experiment")]
mod generator_sum_emit;
#[cfg(any(test, feature = "generator-sum-experiment"))]
mod generator_sum_fields;
#[cfg(test)]
mod generator_sum_plan;
#[cfg(test)]
mod generator_sum_prepared;
#[cfg(any(test, feature = "generator-sum-experiment"))]
mod generator_sum_range_plan;

#[cfg(test)]
mod records_increment_contract_tests;

#[cfg(any(test, feature = "records-increment-experiment"))]
mod records_increment_guard;
#[cfg(any(test, feature = "records-increment-experiment"))]
mod records_increment_plan;
#[cfg(feature = "records-increment-experiment")]
pub use records_increment_guard::{RecordsIncrementExperiment, apply_records_increment_experiment};

mod coroutines;

#[cfg(test)]
mod stock_thin_tests;

mod stock_thin_contract;

#[cfg(test)]
mod unicode_literal_tests;
