"""新metadata只经实际PE/目录审计后投影给旧schema；不修改原manifest或全局验证器。"""
import manifest_verify as legacy
import distribution_metadata_contract as metadata
from contract import require, sha


def validate_projected(data, report, selected, sdk):
    projected = metadata.schema_projection(data, report)
    legacy.validate_schema(projected, 'linux-x64', sdk,
                           expected_instance_modules=tuple(selected))
    return projected


def verify(runner, name, layout, request, selected, sdk):
    """调用方继续逐项核build-input/source；本层保持原artifact全集及唯一ELF检查。"""
    before = sha(layout.manifest)
    if request is None:
        data = legacy.verify_manifest(layout.dist, layout.native, 'linux-x64', sdk,
                                      expected_instance_modules=tuple(selected))
    else:
        report, _ = metadata.audit_pe(runner, name + '-distribution', layout.managed,
            layout.managed.parent / 'distribution-metadata.json', request)
        data = metadata.parse(layout.manifest.read_bytes())
        validate_projected(data, report, selected, sdk)
        actual = legacy.check_artifacts(layout.dist, data['artifacts'], 'ryupy-artifact.json', windows=False)
        entry = next(x for x in data['artifacts'] if x['role'] == 'native_executable')
        native = actual[entry['relative_path']]
        require(layout.native.is_file() and layout.native.samefile(native)
                and layout.native.parent.samefile(native.parent) and layout.native.name == native.name,
                '实际Native不是原清单唯一ELF')
        legacy.image_header(native, 'linux-x64')
    require(sha(layout.manifest) == before, '审计改变原manifest')
    return data
