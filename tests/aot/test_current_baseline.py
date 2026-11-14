"""当前性能基线计划与严格结果门纯单测，不构建或计时产品。"""
import copy
import unittest
from current_baseline import CASES, clean_environment, validate_result


def payload():
    stats=dict.fromkeys(('main_ms','gc0','gc1','gc2','alloc_bytes','heap_bytes','gc_pause_ms','jit_ms','jit_methods'),0)
    series={'wall_ms':[1]*5,'main_ms':[1]*5,'maxrss':[1]*5,'stats':stats}
    return {'environment':{'backend':'aot','python':'3.12.13','dotnet':'10.0.403','runs':5,'warmup':1},
            'results':[{'name':name,'status':'ok','backend':'aot','variants':{},'compile_ms':1,
                        'cpython':copy.deepcopy(series),'ryupy':copy.deepcopy(series)} for name in CASES]}


class BaselineTests(unittest.TestCase):
    def test_valid_and_missing_or_bad_stats_rejected(self):
        validate_result(payload(),'aot')
        for bad in ({},{'main_ms':float('nan')}):
            data=payload();data['results'][0]['ryupy']['stats']=bad
            with self.assertRaises(ValueError):validate_result(data,'aot')
        for invalid in (float('nan'),-1,True):
            data=payload();data['results'][0]['cpython']['wall_ms'][0]=invalid
            with self.assertRaises(ValueError):validate_result(data,'aot')

    def test_plan_drift_and_failed_case_rejected(self):
        for key,value in (('python','3.13.0'),('dotnet','10.0.404'),('runs',6),('backend','jit')):
            data=payload();data['environment'][key]=value
            with self.assertRaises(ValueError):validate_result(data,'aot')
        for mutate in (lambda d:d['results'].pop(),lambda d:d['results'][0].update(status='mismatch'),
                       lambda d:d['results'][0].update(name='calls'),lambda d:d['results'][0].update(variants={'pic0':{}})):
            data=payload();mutate(data)
            with self.assertRaises(ValueError):validate_result(data,'aot')

    def test_experiments_removed_but_sdk_location_kept(self):
        env=clean_environment({'DOTNET_ROOT':'sdk','DOTNET_TieredCompilation':'0','COMPlus_gcServer':'1',
                               'RYUPY_PIC':'0','PYTHONPATH':'poison','PATH':'tools'})
        self.assertEqual(env['DOTNET_ROOT'],'sdk');self.assertEqual(env['PATH'],'tools')
        for key in ('DOTNET_TieredCompilation','COMPlus_gcServer','RYUPY_PIC','PYTHONPATH'):self.assertNotIn(key,env)
        self.assertEqual(env['PYTHONHASHSEED'],'0')


if __name__=='__main__':unittest.main()
