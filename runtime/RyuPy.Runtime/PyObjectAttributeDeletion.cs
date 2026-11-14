// 统一属性删除原语；显式object入口绕过用户hook和frozen限制，保留数据描述符与槽布局守卫。
namespace RyuPy.Runtime;

public static partial class PyObject
{
    /// <summary>删除属性。</summary>
    internal static void DeleteObjectAttribute(object? obj, string name, bool fromObject)
    {
        PyAttributeDeletion.Guard(obj, name, fromObject);
        switch (obj)
        {
            case PyBaseException { ForeignObject: PyNativeObject origin } error:
                origin.DelAttr(name); error.ForeignObject.SynchronizeException(error, false); return;
            case PyModule module:
                PyModuleInstances.DeleteAttr(module, name);
                return;
            case PySlice:
                PySliceMembers.RejectMutation(name, null, deleting: true);
                return;
            case PyType type when ReferenceEquals(type, PySlice.Type):
                PySliceMembers.RejectTypeMutation(name, deleting: true);
                return;
            case PyNamedTupleInstance namedTuple:
                if (PyTypingNamedTupleMembers.TryWriteProperty(namedTuple, name, null, deleting: true)) return;
                PyNamedTupleLayout.RejectInstanceMutation(namedTuple, name, deleting: true);
                return;
            case PyType type when ReferenceEquals(type, PyBaseObject.Type):
                PyBaseObject.RejectTypeMutation(name);
                return;
            case PyNativeObject native:
                native.DelAttr(name);
                return;
            case PyFunction function:
                function.DeleteMetadata(name);
                return;
            case PyClass cls:
                if (!cls.DeleteAttr(name))
                {
                    throw new PyAttributeError(
                        $"type object '{(cls.IsTypingGenericRoot ? "typing.Generic" : cls.Name)}' has no attribute '{name}'"
                    );
                }
                return;
            case PyInstance instance:
                instance.Class.RequireLexicalFactoryReady();
                if (name is "__class__" or "__dict__") PyTypingIo.GuardInstanceReservedMutation(instance.Class, name);
                if (instance.Class.HasProperties
                    && instance.Class.TryLookup(name, out PyClass propertyOwner, out object? descriptor)
                    && descriptor is PyProperty property)
                {
                    if (property.Deleter is null)
                        throw new PyAttributeError($"property '{name}' of '{instance.Class.Name}' object has no deleter");
                    InvokeBound(property.Deleter, propertyOwner, instance, [], [], []);
                    return;
                }
                if (instance.Class.HasSlotLayout)
                {
                    if (instance.Class.TryLookup(name, out _, out object? fixedRaw) && fixedRaw is PyInstanceSlotDescriptor fixedSlot) { fixedSlot.Delete(instance); return; }
                    if (instance.DeleteSlotDictionaryField(name)) return;
                    throw new PyAttributeError($"'{instance.Class.Name}' object has no attribute '{name}'");
                }
                if (instance.Class.IsTypingIoRoot) PyTypingIo.RejectRootInstanceMutation(instance.Class, name);
                if (instance.Class.TryGetInstanceSlot(name, out int slot) && instance.IsSlotSet(slot))
                {
                    instance.ClearSlot(slot);
                    return;
                }
                throw new PyAttributeError(
                    $"'{(instance.Class.IsTypingGenericRoot ? "typing.Generic" : instance.Class.Name)}' object has no attribute '{name}'"
                );
            case PySyntaxError:
                throw PyXmlTreeBuilderErrors.Unsupported("SyntaxError/ParseError attribute mutation");
            case PyGetoptError getopt:
                getopt.Delete(name);
                return;
            case PyOptParseError error:
                error.Delete(name);
                return;
            case PyBaseException exception when exception.PythonClass is not null:
                if (exception.UserAttributes.Remove(name))
                {
                    return;
                }
                throw new PyAttributeError(
                    $"'{exception.PyTypeName}' object has no attribute '{name}'"
                );
            default:
                throw new PyAttributeError(
                    $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
                );
        }
    }

}
