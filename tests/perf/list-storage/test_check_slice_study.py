#!/usr/bin/env python3
"""尺寸诊断证据门的合成正负例，不伪称真实host结果。"""
import unittest
from check_slice_study import check, SIZES, ORDERS


class StudyTests(unittest.TestCase):
    def setUp(self):
        self.data = dict(schema="list-slice-size-study-v1", frequency=1000, framework=".NET 10.0.0",
                         samples=[dict(round=r, mode=m, size=n, start=start,
                                       order_index=ORDERS[r % 6].index(m), repeats=2000,
                                       ticks=1, allocated_bytes=0)
                                  for r in range(12) for m in ORDERS[0]
                                  for n in SIZES for start in (0, 1000)])

    def test_valid(self):
        check(self.data)

    def test_duplicate(self):
        self.data['samples'][0] = self.data['samples'][1]
        with self.assertRaises(AssertionError): check(self.data)

    def test_missing(self):
        self.data['samples'].pop()
        with self.assertRaises(AssertionError): check(self.data)

    def test_order(self):
        self.data['samples'][0]['order_index'] = 2
        with self.assertRaises(AssertionError): check(self.data)

    def test_repeats(self):
        self.data['samples'][0]['repeats'] = 1
        with self.assertRaises(AssertionError): check(self.data)

    def test_old_schema(self):
        self.data['schema'] = 'list-storage-prototype-v3'
        with self.assertRaises(AssertionError): check(self.data)


if __name__ == '__main__':
    unittest.main()
