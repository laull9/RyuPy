# Request、异常层次及默认 SSL 上下文的纯本地合同，不访问外网。
from urllib.request import Request
from urllib.error import HTTPError, URLError
import ssl
import http.client

request = Request("http://example.com/a?q=1#frag", headers={"x-test": "yes"})
print(request.full_url, request.type, request.host, request.selector, request.get_method())
print(request.has_header("X-test"), request.get_header("X-test"), request.header_items())
request.add_header("X-More", "second")
request.remove_header("X-test")
print(request.get_header("X-test", "absent"), request.get_header("X-more"))
request.data = b"a=1"
print(request.get_method(), request.data)
print(Request("https://example.com", method="POST").get_method())
error = URLError("offline")
print(type(error).__name__, error.reason, str(error), error.args)
print(isinstance(error, OSError), issubclass(HTTPError, URLError))
try:
    raise error
except URLError as caught:
    print(caught is error)
context = ssl.create_default_context()
print(context.check_hostname, context.verify_mode == ssl.CERT_REQUIRED)
print(http.client.HTTPResponse.__name__)
