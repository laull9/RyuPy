"""同一编译程序组合随包json provider与直接声明zlib C ABI。"""
import json
import combo_helper
import native_demo

request = json.loads('{"size":4096}')
result = {'bound': native_demo.bound(request['size']),
          'reverse_bound': combo_helper.bound(1024),
          'checksum': native_demo.checksum(0, b'a\x00\xffz')}
print(json.dumps(result, sort_keys=True))
