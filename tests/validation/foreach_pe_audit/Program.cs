// 独立读取真实PE及元数据；不加载/执行目标程序集，不依赖编译器文本dump。
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;

sealed class Names(MetadataReader r) : ISignatureTypeProvider<string, object?> {
    public string Entity(EntityHandle h) => h.Kind switch {
        HandleKind.TypeDefinition => GetTypeFromDefinition(r, (TypeDefinitionHandle)h, 0),
        HandleKind.TypeReference => GetTypeFromReference(r, (TypeReferenceHandle)h, 0),
        HandleKind.TypeSpecification => GetTypeFromSpecification(r, null, (TypeSpecificationHandle)h, 0),
        HandleKind.MethodDefinition => Method((MethodDefinitionHandle)h),
        HandleKind.MemberReference => Member((MemberReferenceHandle)h),
        HandleKind.FieldDefinition => Field((FieldDefinitionHandle)h),
        HandleKind.MethodSpecification => Spec((MethodSpecificationHandle)h),
        HandleKind.AssemblyReference => r.GetString(r.GetAssemblyReference((AssemblyReferenceHandle)h).Name),
        HandleKind.ModuleDefinition => r.GetString(r.GetModuleDefinition().Name),
        _ => throw new Exception("未支持元数据句柄：" + h.Kind)
    };
    public static string Sig(MethodSignature<string> s) => $"[{s.Header.RawValue};{s.GenericParameterCount};{s.RequiredParameterCount}]({string.Join(',', s.ParameterTypes)})->{s.ReturnType}";
    string Method(MethodDefinitionHandle h) { var m=r.GetMethodDefinition(h); return Entity(m.GetDeclaringType())+"::"+r.GetString(m.Name)+Sig(m.DecodeSignature(this,null)); }
    string Member(MemberReferenceHandle h) { var m=r.GetMemberReference(h); return Entity(m.Parent)+"::"+r.GetString(m.Name)+(m.GetKind()==MemberReferenceKind.Method?Sig(m.DecodeMethodSignature(this,null)):m.DecodeFieldSignature(this,null)); }
    string Field(FieldDefinitionHandle h) { var f=r.GetFieldDefinition(h); return Entity(f.GetDeclaringType())+"::"+r.GetString(f.Name)+":"+f.DecodeSignature(this,null); }
    string Spec(MethodSpecificationHandle h) { var s=r.GetMethodSpecification(h); return Entity(s.Method)+"<"+string.Join(',',s.DecodeSignature(this,null))+">"; }
    public string GetArrayType(string e, ArrayShape s)=>e+"["+s.Rank+";"+string.Join(',',s.Sizes)+";"+string.Join(',',s.LowerBounds)+"]";
    public string GetByReferenceType(string e)=>e+"&";
    public string GetFunctionPointerType(MethodSignature<string> s)=>"fn"+Sig(s);
    public string GetGenericInstantiation(string t, ImmutableArray<string> a)=>t+"<"+string.Join(',',a)+">";
    public string GetGenericMethodParameter(object? c,int i)=>"!!"+i;
    public string GetGenericTypeParameter(object? c,int i)=>"!"+i;
    public string GetModifiedType(string m,string u,bool req)=>$"{u} mod({req},{m})";
    public string GetPinnedType(string e)=>e+" pinned";
    public string GetPointerType(string e)=>e+"*";
    public string GetPrimitiveType(PrimitiveTypeCode c)=>c.ToString();
    public string GetSZArrayType(string e)=>e+"[]";
    public string GetTypeFromDefinition(MetadataReader x,TypeDefinitionHandle h,byte k) {var t=x.GetTypeDefinition(h); return x.GetString(t.Namespace)+"."+x.GetString(t.Name);}
    public string GetTypeFromReference(MetadataReader x,TypeReferenceHandle h,byte k) {var t=x.GetTypeReference(h); return "["+Entity(t.ResolutionScope)+"]"+x.GetString(t.Namespace)+"."+x.GetString(t.Name);}
    public string GetTypeFromSpecification(MetadataReader x,object? c,TypeSpecificationHandle h,byte k)=>x.GetTypeSpecification(h).DecodeSignature(this,c);
}
record Ins(int Offset, string Op, object? Arg, string Raw, string Pop, string Push, int? CallArgs, bool? CallReturns);
class Program {
    static readonly Dictionary<ushort,OpCode> Ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>unchecked((ushort)o.Value));
    static List<Ins> Decode(byte[] bytes,MetadataReader r,Names names) {
        var result=new List<Ins>(); int p=0;
        int I32(){var n=BitConverter.ToInt32(bytes,p);p+=4;return n;}
        while(p<bytes.Length) {
            int start=p; ushort code=bytes[p++]; if(code==0xfe)code=(ushort)(0xfe00|bytes[p++]);
            var op=Ops[code]; object? arg=null; int? argc=null; bool? returns=null;
            switch(op.OperandType) {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineI: arg=(int)(sbyte)bytes[p++];break;
                case OperandType.InlineI: arg=I32();break;
                case OperandType.InlineI8: arg=BitConverter.ToInt64(bytes,p).ToString();p+=8;break;
                case OperandType.ShortInlineR: arg=Convert.ToHexString(bytes.AsSpan(p,4));p+=4;break;
                case OperandType.InlineR: arg=Convert.ToHexString(bytes.AsSpan(p,8));p+=8;break;
                case OperandType.ShortInlineVar: arg=(int)bytes[p++];break;
                case OperandType.InlineVar: arg=(int)BitConverter.ToUInt16(bytes,p);p+=2;break;
                case OperandType.ShortInlineBrTarget: {int delta=(sbyte)bytes[p++];arg=p+delta;break;}
                case OperandType.InlineBrTarget: {int delta=I32();arg=p+delta;break;}
                case OperandType.InlineSwitch: {int n=I32();var d=new int[n];for(int j=0;j<n;j++)d[j]=I32();arg=d.Select(v=>v+p).ToArray();break;}
                case OperandType.InlineString: arg=r.GetUserString(MetadataTokens.UserStringHandle(I32()&0xffffff));break;
                case OperandType.InlineSig: arg=Convert.ToHexString(r.GetBlobBytes(r.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(I32()&0xffffff)).Signature));break;
                default: {
                    var h=MetadataTokens.EntityHandle(I32());arg=names.Entity(h);
                    if(op.FlowControl==FlowControl.Call || op.Name=="newobj") {
                        if(h.Kind==HandleKind.MethodSpecification) h=r.GetMethodSpecification((MethodSpecificationHandle)h).Method;
                        var s=h.Kind==HandleKind.MethodDefinition?r.GetMethodDefinition((MethodDefinitionHandle)h).DecodeSignature(names,null):r.GetMemberReference((MemberReferenceHandle)h).DecodeMethodSignature(names,null);
                        argc=s.ParameterTypes.Length+(s.Header.IsInstance && op.Name!="newobj"?1:0);
                        returns=op.Name=="newobj"||s.ReturnType!="Void";
                    }
                    break;
                }
            }
            result.Add(new(start,op.Name!,arg,Convert.ToHexString(bytes.AsSpan(start,p-start)),op.StackBehaviourPop.ToString(),op.StackBehaviourPush.ToString(),argc,returns));
        }
        return result;
    }
    static void Main(string[] args) {
        using var pe=new PEReader(File.OpenRead(args[0])); var r=pe.GetMetadataReader();var names=new Names(r);
        var methods=new List<object>();
        foreach(var h in r.MethodDefinitions) {
            var m=r.GetMethodDefinition(h); var b=m.RelativeVirtualAddress==0?null:pe.GetMethodBody(m.RelativeVirtualAddress);
            var il=b?.GetILBytes()??[];
            int? ilFileOffset=null;
            if(b!=null) {
                var section=pe.PEHeaders.SectionHeaders.Single(s=>m.RelativeVirtualAddress>=s.VirtualAddress && m.RelativeVirtualAddress<s.VirtualAddress+Math.Max(s.VirtualSize,s.SizeOfRawData));
                var bodyOffset=m.RelativeVirtualAddress-section.VirtualAddress+section.PointerToRawData;
                var header=pe.GetSectionData(m.RelativeVirtualAddress).GetContent(0,2);
                ilFileOffset=bodyOffset+((header[0]&3)==2?1:((header[0]|header[1]<<8)>>12)*4);
            }
            methods.Add(new {Name=names.Entity(h), Attributes=(int)m.Attributes, ImplAttributes=(int)m.ImplAttributes,
                Signature=Names.Sig(m.DecodeSignature(names,null)), MaxStack=b?.MaxStack, InitLocals=b?.LocalVariablesInitialized,
                Locals=b==null||b.LocalSignature.IsNil?[]:r.GetStandaloneSignature(b.LocalSignature).DecodeLocalSignature(names,null).ToArray(),
                EH=b?.ExceptionRegions.Select(e=>new {Kind=e.Kind.ToString(),e.TryOffset,e.TryLength,e.HandlerOffset,e.HandlerLength,e.FilterOffset,Catch=e.CatchType.IsNil?null:names.Entity(e.CatchType)}).ToArray(),
                ILFileOffset=ilFileOffset, IL=Decode(il,r,names), Bytes=Convert.ToHexString(il)});
        }
        Console.WriteLine(JsonSerializer.Serialize(new {Path=Path.GetFullPath(args[0]),Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(args[0]))),Methods=methods},new JsonSerializerOptions{WriteIndented=true}));
    }
}
