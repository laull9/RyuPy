# 本地网络纵切客户端：同一源码依次交 CPython、CIL 和搬迁后的 NativeAOT 执行。
import sys
import ssl
import http.client
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode

base, secure, wrong_host, ca, refused, mode = sys.argv[1:]

if mode == "boundaries":
    context = ssl.create_default_context(cafile=ca)
    for action in [lambda: ssl.create_default_context(capath="."),
                   lambda: context.load_verify_locations(capath="."),
                   lambda: ssl.SSLContext(),
                   lambda: urlopen(base, timeout=0),
                   lambda: urlopen(base, cafile=ca),
                   lambda: urlopen("file:///etc/passwd"),
                   lambda: urlopen(Request(base, headers={"Content-Length": "99"})),
                   lambda: urlopen(Request(base, headers={"Transfer-Encoding": "chunked"}))]:
        try:
            action()
        except (NotImplementedError, URLError):
            print("explicit rejection")
        else:
            raise AssertionError("unsupported configuration accepted")
    for attribute, value in [("check_hostname", False), ("verify_mode", ssl.CERT_NONE), ("minimum_version", 0)]:
        try:
            setattr(context, attribute, value)
        except NotImplementedError:
            print("SSL setting rejected", attribute)
        else:
            raise AssertionError("SSL setting silently accepted")
    request = Request(base + "/cross", headers={"Authorization": "secret", "Cookie": "secret", "X-Safe": "yes"})
    with urlopen(request, timeout=2) as response:
        print("cross origin", response.read())
    try:
        response = urlopen(secure + "/downgrade", context=context, timeout=2)
    except URLError:
        print("HTTPS downgrade rejected")
    else:
        response.close()
        raise AssertionError("HTTPS downgrade accepted")
else:
    with urlopen(base + "/get?x=one%20two", timeout=2) as response:
        print("GET", response.status, response.getcode(), response.reason, response.version)
        print(isinstance(response, http.client.HTTPResponse), response.geturl().endswith("/get?x=one%20two"))
        print(response.info() is response.headers, response.headers["x-test"], response.getheader("missing", "default"))
        print(response.headers.get_all("X-Multi"), "x-test" in response.headers)
        print(response.getheader("X-Multi"), response.headers.get("missing"), response.headers.get_all("missing", []))
        print(response.read(3), response.read(), response.read(), response.closed)
    print("closed", response.closed, response.read())
    response.close()
    payload = urlencode({"word": "hello world", "中文": "内容"}).encode()
    with urlopen(base + "/target/a/../b%2Fc?q=%2F#ignored", timeout=2) as response:
        print("raw target", response.read())
    request = Request(base + "/post", data=payload, headers={"x-test": "first"})
    request.headers["X-test"] = "mutated"
    with urlopen(request, timeout=2) as response:
        print("POST", response.read() == b"POST|mutated|" + payload)
    for code in [301, 302, 303]:
        with urlopen(Request(base + "/redirect/" + str(code), data=b"a=1"), timeout=2) as response:
            print("redirect POST", code, response.read())
    for code in [307, 308]:
        with urlopen(base + "/redirect/" + str(code), timeout=2) as response:
            print("redirect GET", code, response.read())
        try:
            urlopen(Request(base + "/redirect/" + str(code), data=b"a=1"), timeout=2)
        except HTTPError as error:
            print("redirect POST refused", error.code)
            error.close()
    request = Request(base + "/get", method="HEAD")
    with urlopen(request, timeout=2) as response:
        print("HEAD", response.status, response.read())
    try:
        with urlopen(base + "/get", timeout=2) as response:
            raise ValueError("cleanup")
    except ValueError:
        print("exception cleanup", response.closed)
    for path in ["/error", "/loop"]:
        try:
            urlopen(base + path, timeout=2)
        except HTTPError as error:
            with error as body:
                print("HTTPError", isinstance(error, URLError), isinstance(error, OSError), error.code, error.status)
                print(body is error, error.headers["X-Test"], error.geturl().endswith(path), error.args)
                print(error.read())
            print("HTTPError closed", error.closed)
    try:
        urlopen(refused, timeout=0.2)
    except URLError as error:
        print("connection refused", isinstance(error.reason, OSError))
    for path in ["/slow-header", "/slow-body"]:
        try:
            with urlopen(base + path, timeout=0.05) as response:
                response.read()
        except TimeoutError as error:
            print("timeout", path, str(error), isinstance(error, OSError))
    try:
        with urlopen(base + "/truncated", timeout=2) as response:
            response.read()
    except http.client.IncompleteRead as error:
        print("truncated", error.partial, error.expected)
    with urlopen(base + "/truncated", timeout=2) as response:
        print("short read", response.read(8))
        try:
            response.read()
        except http.client.IncompleteRead as error:
            print("remaining truncated", error.partial, error.expected)
    try:
        urlopen(secure + "/get", timeout=2)
    except URLError as error:
        print("default TLS rejects private CA", isinstance(error.reason, ssl.SSLCertVerificationError))
    context = ssl.create_default_context(cafile=ca)
    with urlopen(secure + "/get", context=context, timeout=2) as response:
        print("trusted TLS", response.status, response.read())
    try:
        urlopen(wrong_host + "/get", context=context, timeout=2)
    except URLError as error:
        print("TLS hostname", isinstance(error.reason, ssl.SSLCertVerificationError))
    with open(ca) as certificate:
        context = ssl.create_default_context(cadata=certificate.read())
    with urlopen(secure + "/get", context=context, timeout=2) as response:
        print("PEM TLS", response.read())
    context = ssl.create_default_context()
    context.load_verify_locations(cafile=ca)
    with urlopen(secure + "/get", context=context, timeout=2) as response:
        print("loaded CA TLS", response.read())
