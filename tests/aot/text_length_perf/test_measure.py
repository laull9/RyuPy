"""分布计算轻量合同；不冒充真实Native测量。"""
import unittest
from run import distribution

class DistributionTests(unittest.TestCase):
    def test_sorted_interpolated_distribution(self):
        actual=distribution([4,1,3,2])
        self.assertEqual(actual['n'],4)
        self.assertEqual(actual['median_ms'],2.5)
        self.assertEqual(actual['p25_ms'],1.75)
        self.assertEqual(actual['p75_ms'],3.25)
        self.assertEqual(actual['min_ms'],1)
        self.assertEqual(actual['max_ms'],4)
    def test_insufficient_sample_rejected(self):
        with self.assertRaises(ValueError):distribution([1])

if __name__=='__main__':unittest.main()
