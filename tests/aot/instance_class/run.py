"""显式普通类AOT竖切：共享编译工具，先实际PE/managed门，再同PE Native与搬迁。"""
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
from contract import anchors, parse, require, sha, verify_sources, verify_plan, verify_manifest, no_resources
from support import ROOT, Runner, old
from pe_contract import check

HERE = Path(__file__).resolve().parent
TITLE = 'validation: instance-class-aot-001'


def context():
    event = json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text())
    require(os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted' and
            os.environ.get('GITHUB_EVENT_NAME') == 'push' and
            os.environ.get('GITHUB_REF') == 'refs/heads/ci/wcwidth-legacy-aot-2909056' and
            event.get('head_commit', {}).get('message') == TITLE, '只允许精确同源hosted竖切')
    require('RYUPY_INSTANCE_CLASS_SOURCE_MANIFEST' not in os.environ, 'hosted禁止借本地身份替代commit')


def source_identity(r, label):
    r.checked(label, ['git', 'ls-files', '-s', '-z'])
    raw = (r.out / (label + '.stdout')).read_bytes()
    records = []
    for line in raw.split(b'\0'):
        if not line: continue
        fields, path = line.split(b'\t', 1)
        mode, blob, stage = fields.decode().split()
        require(stage == '0', '未合并index')
        p = ROOT / os.fsdecode(path)
        require(p.is_file() and not p.is_symlink(), 'source不是普通文件')
        records.append({'path': os.fsdecode(path), 'mode':mode,'blob':blob,'sha256':sha(p)})
    r.save(label + '-files.json', records)
    return records


def prepare(r):
    r.checked('head', ['git','rev-parse','HEAD'])
    require((r.out/'head.stdout').read_text().strip() == os.environ['GITHUB_SHA'], 'HEAD身份不符')
    r.before = source_identity(r, 'source-before')
    r.freeze([ROOT/x['path'] for x in r.before])
    r.checked('format', ['cargo','fmt','--all','--check'],120)
    r.checked('runtime-build', ['dotnet','build','runtime/RyuPy.Runtime/RyuPy.Runtime.csproj',
                             '-c','Release','-nologo'],300)
    r.checked('cli-build',['cargo','build','--locked','-p','ryupy-cli',
                          '--features','ryupy-driver/ordinary-dynamic-module-pilot'],1200)
    tools=r.out/'tools'; tools.mkdir()
    r.cli=tools/'ryupy'; r.runtime=tools/'RyuPy.Runtime.dll'
    shutil.copy2(ROOT/'target/debug/ryupy',r.cli)
    shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll',r.runtime)
    r.env.update(RYUPY_RUNTIME_DIR=str(tools), DOTNET=str(r.dotnet), DOTNET_ROOT=str(r.dotnet.parent))
    r.freeze([r.cli,r.runtime,r.python,r.dotnet])
    r.inputs_dir=r.inputs()
    shutil.copy2(ROOT/'tests/fixtures/treebuilder/markdown_html.py', r.inputs_dir/'markdown_html.py')
    r.fixture=r.out/'fixtures'; shutil.copytree(HERE/'fixtures',r.fixture)
    r.goldens=parse((r.fixture/'EXPECTED.json').read_bytes())
    r.request=r.inputs_dir/'metadata-request.json'
    old.save(r.request, {'schema_version':1,'mode':'closed_snapshot','roots':[{'id':'markdown','path':'markdown'}]})
    source_map=json.loads((ROOT/'tests/validation/text_search.json').read_text())['markdown_sources']
    verify_sources(r.inputs_dir,source_map)
    r.cases=[]
    for name,filename,expected in [
        ('markdown_sample','markdown_sample.py',(r.inputs_dir/'stdout.txt').read_bytes()),
        ('markdown_html','markdown_html.py',(ROOT/'tests/fixtures/treebuilder/oracle/markdown-html.stdout').read_bytes())]:
        require(len(expected)==(77 if name=='markdown_sample' else 102),'原业务黄金字节数变化')
        r.cases.append({'name':name,'entry':r.inputs_dir/filename,'sources':source_map,
                        'source_root':r.inputs_dir,'selected':['html.parser'],'search':[r.inputs_dir/'stdlib',r.inputs_dir/'markdown'],
                        'include':list(old.INCLUDES),'metadata':r.request,'executions':[([],expected)]})
    fixture_sources={name:{'path':name+'.py','sha256':sha(r.fixture/(name+'.py'))} for name in ('base','subject')}
    for name, modes in [('entry_capture',['copies','builtins','base','body','default']),('hooks_product',['init-subclass','set-name'])]:
        r.cases.append({'name':name,'entry':r.fixture/(name+'.py'),'sources':fixture_sources,
                        'source_root':r.fixture,'selected':['subject'],'search':[r.fixture],'include':['subject'],
                        'metadata':None,'executions':[([mode],r.goldens['products'][mode].encode()) for mode in modes]})
    requests=[]
    for case in r.cases:
        resource=r.out/(case['name']+'-resources.json'); old.save(resource,anchors(case['sources']))
        case['resources']=resource
        requests.append({'name':case['name'],'entry':str(case['entry']),
            'search_paths':list(map(str,case['search'])),'selected':case['selected'],
            'include_modules':case['include'],'resources':str(resource),
            'distribution_metadata':str(case['metadata']) if case['metadata'] else None})
    old.save(r.out/'requests.json',requests)
    r.freeze([p for root in (r.inputs_dir,r.fixture) for p in root.rglob('*') if p.is_file()])
    r.freeze([r.out/'requests.json', *(case['resources'] for case in r.cases)])


def argv(r,case,target,aot=False,resources=True):
    args=[r.cli,'build',case['entry'],'--diagnostic-stages','--out-dir',target]
    for path in case['search']:args += ['-I',path]
    for name in case['selected']:args += ['--instance-module',name]
    for name in case['include']:args += ['--include-module',name]
    if case['metadata']:args += ['--distribution-metadata',case['metadata']]
    if aot:args += ['--aot']
    if resources:args += ['--resources',case['resources']]
    return args


def original_rejection(r):
    case=r.cases[0]; target=r.out/'missing-resources'
    record=r.command('missing-resources',argv(r,case,target,aot=True,resources=False),180)
    no_resources(record,(r.out/'missing-resources.stdout').read_bytes(),
                 (r.out/'missing-resources.stderr').read_bytes(),target)


def oracle(r):
    for case in r.cases:
        # hooks_product是明确NI产品观察器，CP使用原真实entry，保两套不同黄金。
        hook = case['name'] == 'hooks_product'
        entry = r.fixture/'entry.py' if hook else case['entry']
        env = {'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC','PYTHONHASHSEED':'0',
               'PYTHONDONTWRITEBYTECODE':'1','PYTHONNOUSERSITE':'1',
               'PYTHONPATH':os.pathsep.join(map(str,case['search']))}
        from contract import positive
        for number,(arguments,golden) in enumerate(case['executions']):
            if hook: golden=r.goldens['cp'][arguments[0]].encode()
            label=case['name']+'-cp-'+str(number)
            record=r.command(label,[r.python,'-S','-B',entry,*arguments],30,env)
            positive(record,(r.out/(label+'.stdout')).read_bytes(),(r.out/(label+'.stderr')).read_bytes(),golden)


def structure(r):
    output=r.out/'structure'
    r.env.update(RYUPY_INSTANCE_CLASS_REQUEST=str(r.out/'requests.json'),RYUPY_INSTANCE_CLASS_OUT=str(output))
    r.named_test('structure',['cargo','test','--locked','-p','ryupy-driver','--features','ordinary-dynamic-module-pilot',
          'instance_class_aot_real_source_and_pe_gate','--','--ignored','--nocapture'],
          'instance_class_aot_real_source_and_pe_gate',1200)
    sys.path.insert(0,str(ROOT/'tests/aot/wcwidth_ci'))
    from instance_pe import envelope, unchanged
    r.pe_identity={}
    for case in r.cases:
        name=case['name']; r.pe_identity.update(envelope(output,name,os.environ['GITHUB_SHA']))
        expected=parse((output/(name+'-expected.json')).read_bytes())
        mapping=parse((output/(name+'-methods.json')).read_bytes())
        verify_plan(parse((output/(name+'-plan.json')).read_bytes()),case['sources'],case['selected'])
        decoder=ROOT/'tests/validation/source_provenance/ci/read_pe_decoder.py'
        r.checked(name+'-decode',[r.python,'-B',decoder,output/(name+'.dll'),'old',name],60)
        decoded=parse((r.out/(name+'-decode.stdout')).read_bytes())
        sources={module:{'text':(case['source_root']/case['sources'][module]['path']).read_text(),
                         'sha256':case['sources'][module]['sha256']} for module in case['selected']}
        audit=check(decoded,expected,mapping,sources)
        if name.startswith('markdown_'):
            require(sum(x['lexical_qualname'].startswith('HTMLParser.') for x in mapping)==26,'26方法全集丢失')
        r.save(name+'-actual-pe.json',audit)
    r.env.update(RYUPY_INSTANCE_AOT_STRUCTURE_OUT=str(output),
                 RYUPY_INSTANCE_CLASS_CASES='\n'.join(c['name'] for c in r.cases))
    r.named_test('pe-signatures',['cargo','test','--locked','-p','ryupy-codegen-cil',
       'instance_class_actual_pe_gate','--','--ignored','--nocapture'],'instance_class_actual_pe_gate',900)
    unchanged(output,r.pe_identity)


def managed_kind2_boundary(r):
    from contract import kind2_rejection
    folder=r.out/'structure'; shutil.copy2(r.runtime,folder/'RyuPy.Runtime.dll')
    pe=folder/'entry_capture.dll'
    old.save(folder/'entry_capture.runtimeconfig.json',{'runtimeOptions':{'tfm':'net10.0',
        'framework':{'name':'Microsoft.NETCore.App','version':'10.0.14'}}})
    record=r.command('kind2-managed-rejection',[r.dotnet,pe,'copies'],30)
    kind2_rejection(record,(r.out/'kind2-managed-rejection.stdout').read_bytes(),
                   (r.out/'kind2-managed-rejection.stderr').read_bytes())
    r.save('kind2-boundary.json',{'status':'expected-rejection','native_accepted':False,
       'scope':'只证明普通dotnet拒ExecutableRelative；不是CP等价或Native失败'})


def managed(r):
    # kind2只准Native；普通CLI JIT是独立source-absolute控制，不假称同PE或新hook守卫执行。
    for case in r.cases:
        name=case['name']
        if name=='hooks_product':
            r.save('hooks-managed-boundary.json',{'status':'not-run','reason':
                '普通JIT不启用新AOT guard；aot=true的kind2 PE不能在dynamic-code runtime执行'})
            continue
        target=r.out/'jit'/name
        r.checked(name+'-jit-build',argv(r,case,target,aot=False,resources=False),180)
        pe=target/(case['entry'].stem+'.dll')
        require(pe.is_file() and sha(target/'RyuPy.Runtime.dll')==sha(r.runtime),'普通JIT输入身份不符')
        r.freeze([pe,target/'RyuPy.Runtime.dll'])
        for number,(arguments,golden) in enumerate(case['executions']):
            r.run_positive(name+'-jit-'+str(number),[r.dotnet,pe,*arguments],golden)


def actual_cli_pe(r, case, layout):
    from contract import pe_timestamp_equivalence
    from instance_pe import envelope, unchanged
    name=case['name']; original=r.out/'structure'/(name+'.dll')
    receipt=pe_timestamp_equivalence(original.read_bytes(),layout.managed.read_bytes())
    r.save(name+'-coff-equivalence.json',receipt)
    folder=r.out/(name+'-actual-cli-pe');folder.mkdir()
    shutil.copy2(layout.managed,folder/(name+'.dll'))
    source_expected=parse((r.out/'structure'/(name+'-expected.json')).read_bytes())
    expected=json.loads(json.dumps(source_expected))
    expected['pe']={'name':name+'.dll','size':layout.managed.stat().st_size,'sha256':sha(layout.managed)}
    require({k:v for k,v in expected.items() if k!='pe'}=={k:v for k,v in source_expected.items() if k!='pe'},
            '派生期望除PE身份外改变')
    old.save(folder/(name+'-expected.json'),expected)
    identities=envelope(folder,name,os.environ['GITHUB_SHA'])
    decoder=ROOT/'tests/validation/source_provenance/ci/read_pe_decoder.py'
    r.checked(name+'-cli-decode',[r.python,'-B',decoder,layout.managed,'old',name],60)
    decoded=parse((r.out/(name+'-cli-decode.stdout')).read_bytes())
    mapping=parse((r.out/'structure'/(name+'-methods.json')).read_bytes())
    sources={module:{'text':(case['source_root']/case['sources'][module]['path']).read_text(),
                     'sha256':case['sources'][module]['sha256']} for module in case['selected']}
    r.save(name+'-cli-actual-pe.json',check(decoded,expected,mapping,sources))
    r.env.update(RYUPY_INSTANCE_AOT_STRUCTURE_OUT=str(folder),RYUPY_INSTANCE_CLASS_CASES=name)
    r.named_test(name+'-cli-pe-signatures',['cargo','test','--locked','-p','ryupy-codegen-cil',
        'instance_class_actual_pe_gate','--','--ignored','--nocapture'],'instance_class_actual_pe_gate',900)
    unchanged(folder,identities)


def native(r):
    for case in r.cases:
        name=case['name']; target=r.out/'native-build'/name; label=name+'-publish'
        record=r.command(label,argv(r,case,target,aot=True),1200)
        saved=r.out/(name+'-publish-inputs'); saved.mkdir()
        wanted={'Program.cs','sdk.version','publish.stdout.log','publish.stderr.log',
                'module-environment.json','distribution-metadata.json','ryupy-artifact.json'}
        for path in target.rglob('*') if target.exists() else []:
            if not path.is_file() or any(x in {'obj','bin','publish'} for x in path.relative_to(target).parts): continue
            if path.name in wanted or path.suffix in {'.csproj','.dll'}:
                dest=saved/path.relative_to(target); dest.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,dest)
        require(record['exit']==0 and not record['failure'] and record['raw_streams_complete'],label+'失败，保留本次原件')
        layout=r.native_layout(label,target,case['entry'])
        actual_cli_pe(r,case,layout)
        verify_plan(parse((layout.managed.parent/'module-environment.json').read_bytes()),case['sources'],case['selected'])
        import manifest_verify
        sdk=manifest_verify.read_sdk_version(layout.host/'sdk.version')
        require(sdk=='10.0.403','SDK版本改变')
        from native_manifest import verify as verify_native_manifest
        manifest=verify_native_manifest(r,name,layout,case['metadata'],case['selected'],sdk)
        for item in manifest['build_inputs']:
            path=layout.managed if item['role']=='compiled_managed_input' else r.runtime
            require(path.is_file() and path.stat().st_size==item['size'] and sha(path)==item['sha256'],
                    'Native清单与实际托管输入不符')
        source_hashes={module:row['sha256'] for module,row in case['sources'].items()}
        source_hashes['__main__']=sha(case['entry'])
        require(len(manifest['sources'])==len(source_hashes) and
                {row['module_name']:row['compiled_text_sha256'] for row in manifest['sources']}==source_hashes,
                'Native清单源码身份不符')
        verify_manifest(manifest,case['sources'],case['selected'],layout.dist)
        with r.isolated_sources([r.inputs_dir,r.fixture],name):
            for number,(arguments,golden) in enumerate(case['executions']):
                r.native_pair(name+'-'+str(number),layout,golden,arguments)


def main():
    context(); r=Runner()
    try:
        for name,phase in [('prepare',prepare),('original-no-resources',original_rejection),
                           ('oracle',oracle),('structure',structure),('kind2-managed-boundary',managed_kind2_boundary),('managed',managed),('native',native)]:
            r.phase(name,lambda:phase(r))
        require(source_identity(r,'source-after')==r.before,'全源前后变化')
        r.result['passed']=True
    finally:
        r.save('result.json',r.result)


if __name__=='__main__':
    try:main()
    except Exception:
        traceback.print_exc();sys.exit(1)
