"""发布选项的必需字段与显式扩展；接受已登记冷选项，未知键和错误类型仍拒绝。"""
from manifest_files import ManifestError

BOOL_OPTIONS = frozenset(('experimental_direct_accumulator', 'experimental_foreach_accumulator',
                         'experimental_leaf_inline', 'diagnostic_scalar_slp', 'experimental_packed_slp'))
OPTION_KEYS = BOOL_OPTIONS | {'instance_modules', 'module_environment_policy'}
OPTIONAL_BOOL_OPTIONS = frozenset(('experimental_records_increment',
                                  'experimental_stock_thin_functions',
                                  'experimental_stock_module_namespaces'))
# 与ModuleEnvironmentPolicy.identity的三个生产者值一致；不放行任意未知策略。
POLICIES = frozenset(('explicit-only', 'catalog-only', 'ordinary-dynamic-module-pilot'))


def validate_options(options):
    if (not isinstance(options, dict) or not OPTION_KEYS <= set(options)
            or set(options) - OPTION_KEYS - OPTIONAL_BOOL_OPTIONS - {'distribution_metadata'}):
        raise ManifestError('compiler.options缺少必需字段或包含未登记扩展')
    if any(type(options[key]) is not bool
           for key in BOOL_OPTIONS | (OPTIONAL_BOOL_OPTIONS & set(options))):
        raise ManifestError('优化开关必须是bool')
    if 'distribution_metadata' in options and not isinstance(options['distribution_metadata'], dict):
        raise ManifestError('distribution_metadata必须是冻结的元数据报告对象')
    modules = options['instance_modules']
    # Session允许重复的显式选择；这里只验真实序列/字符串，不凭空去重或扩大选择能力。
    if type(modules) is not list or any(type(name) is not str or not name for name in modules):
        raise ManifestError('instance_modules必须是非空字符串组成的列表（空列表允许）')
    if type(options['module_environment_policy']) is not str or options['module_environment_policy'] not in POLICIES:
        raise ManifestError('未知module_environment_policy')
