"""配对统计与真实负载合同；轻量测试不替代Native A/B实测。"""
import math
import unittest
from run import distribution, paired_effect, WORKLOADS


class MeasurementTests(unittest.TestCase):
    def test_distribution_uses_all_samples(self):
        result=distribution([4,1,3,2])
        self.assertEqual(result['n'],4)
        self.assertEqual(result['median_ms'],2.5)
        self.assertEqual(result['p25_ms'],1.75)
        self.assertEqual(result['p75_ms'],3.25)
        self.assertEqual(result['min_ms'],1)
        self.assertEqual(result['max_ms'],4)
        with self.assertRaises(ValueError):distribution([1])

    def test_pairing_rejects_missing_or_bad_samples(self):
        for before,after in [([1],[1]),([1,2],[1]),([0,1],[1,1]),
                             ([1,math.nan],[1,1]),([1,math.inf],[1,1]),([1,2],[-1,1])]:
            with self.assertRaises(ValueError):paired_effect(before,after,100)
        with self.assertRaises(ValueError):paired_effect([1,2],[1,2],10)

    def test_equal_or_tiny_difference_is_not_improvement(self):
        for scale in (1,1.01):
            result=paired_effect([scale,scale],[1,1],100)
            self.assertEqual(result['signal'],'inconclusive')
        self.assertEqual(paired_effect([1,1],[1,1],100)['bootstrap_95pct_ci'],[1,1])

    def test_constant_ratios_have_exact_direction(self):
        result=paired_effect([2,4,6,8],[1,2,3,4],100)
        self.assertEqual(result['signal'],'improvement_candidate')
        self.assertAlmostEqual(result['geometric_mean_before_over_after'],2)
        self.assertEqual(result['bootstrap_95pct_ci'],[2,2])
        reverse=paired_effect([1,2,3,4],[2,4,6,8],100)
        self.assertEqual(reverse['signal'],'regression_signal')
        self.assertEqual(reverse['bootstrap_95pct_ci'],[.5,.5])

    def test_bootstrap_is_reproducible_and_preserves_pairs(self):
        result=paired_effect([1,2,4,8],[1,1,8,4],500)
        self.assertEqual(result,paired_effect([1,2,4,8],[1,1,8,4],500))
        self.assertLess(result['bootstrap_95pct_ci'][0],1)
        self.assertGreater(result['bootstrap_95pct_ci'][1],1)
        self.assertEqual(result['signal'],'inconclusive')

    def test_workload_scope_keeps_startup_short_long_and_words(self):
        self.assertEqual(list(WORKLOADS),['startup','hi','longhi100','longhi800','paragraph'])
        self.assertEqual(WORKLOADS['startup'],['--version'])
        self.assertEqual(WORKLOADS['hi'],['-f','standard','hi'])
        self.assertEqual(WORKLOADS['longhi100'],['-f','standard','-w','20000','hi'*100])
        self.assertEqual(WORKLOADS['longhi800'],['-f','standard','-w','20000','hi'*800])
        self.assertEqual(WORKLOADS['paragraph'],['-f','standard','-w','80',('hello world from RyuPy '*10).strip()])


if __name__=='__main__':unittest.main()
