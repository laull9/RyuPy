"""稳定native模块限定ParseError派生拒绝。"""
import xml.etree.ElementTree as ET
class Child(ET.ParseError):
    pass
