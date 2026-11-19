# 固定 CPython colorsys 六个公开 API 的分支、往返、截断与错误合同。
import colorsys


def rounded(values):
    return tuple(round(value, 9) for value in values)


samples = [(0.0, 0.0, 0.0), (1.0, 1.0, 1.0), (0.5, 0.5, 0.5),
           (1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (0.0, 0.0, 1.0),
           (0.2, 0.4, 0.8), (0.9, 0.8, 0.1), (0.8, 0.1, 0.9)]
for rgb in samples:
    for forward, backward in [(colorsys.rgb_to_yiq, colorsys.yiq_to_rgb),
                              (colorsys.rgb_to_hls, colorsys.hls_to_rgb),
                              (colorsys.rgb_to_hsv, colorsys.hsv_to_rgb)]:
        converted = forward(*rgb)
        print(forward.__name__, rounded(converted), rounded(backward(*converted)))
for hue in [-1.0, 0.0, 1 / 12, 1 / 6, 1 / 3, 0.5, 2 / 3, 5 / 6, 1.0, 2.0]:
    print('hls_to_rgb', hue, rounded(colorsys.hls_to_rgb(hue, 0.2, 0.7)),
          rounded(colorsys.hls_to_rgb(hue, 0.8, 0.7)),
          rounded(colorsys.hls_to_rgb(hue, 0.4, 0.0)))
    print('hsv_to_rgb', hue, rounded(colorsys.hsv_to_rgb(hue, 0.7, 0.8)),
          rounded(colorsys.hsv_to_rgb(hue, 0.0, 0.8)))
print('yiq_to_rgb', rounded(colorsys.yiq_to_rgb(-1.0, 0.0, 0.0)),
      rounded(colorsys.yiq_to_rgb(2.0, 0.0, 0.0)))
for api in [colorsys.rgb_to_yiq, colorsys.yiq_to_rgb, colorsys.rgb_to_hls,
            colorsys.hls_to_rgb, colorsys.rgb_to_hsv, colorsys.hsv_to_rgb]:
    try:
        api('bad', 0.2, 0.3)
    except TypeError:
        print(api.__name__, 'TypeError')
import colorsys as same_colorsys
print('identity', colorsys is same_colorsys)
