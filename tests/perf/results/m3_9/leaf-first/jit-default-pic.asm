; Assembly listing for method RyuPy.Runtime.PyPic:CallValue1(int,System.Object,System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x04], edi
       mov      gword ptr [rbp-0x10], rsi
       mov      gword ptr [rbp-0x18], rdx
 
G_M000_IG02:
       mov      edi, dword ptr [rbp-0x04]
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:ArgFor(int,int):System.Object[]]
       mov      gword ptr [rbp-0x20], rax
       mov      rdi, gword ptr [rbp-0x20]
       mov      rdx, gword ptr [rbp-0x18]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      edi, dword ptr [rbp-0x04]
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdx, gword ptr [rbp-0x20]
       call     [RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 85

; Assembly listing for method RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; fully interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 192
       lea      rbp, [rsp+0xC0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      dword ptr [rbp-0x2C], edi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0xA0], 0x3E8
       movzx    rax, byte  ptr [(reloc)]
       test     eax, eax
       je       SHORT G_M000_IG04
       cmp      dword ptr [rbp-0x2C], 0x1000
       jb       SHORT G_M000_IG06
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x78], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0xA8], rax
       mov      rcx, gword ptr [rbp-0xA8]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x78]
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       nop      
 
G_M000_IG05:
       add      rsp, 192
       pop      rbp
       ret      
 
G_M000_IG06:
       mov      eax, dword ptr [rbp-0x2C]
       shl      eax, 2
       mov      dword ptr [rbp-0x44], eax
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyPic:KindOfCallable(System.Object):byte]
       mov      dword ptr [rbp-0x48], eax
       xor      eax, eax
       mov      dword ptr [rbp-0x4C], eax
       jmp      G_M000_IG14
 
G_M000_IG07:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [rbp-0x44]
       add      ecx, dword ptr [rbp-0x4C]
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG20
       mov      edx, ecx
       imul     rdx, rdx, 16
       lea      rax, bword ptr [rax+rdx+0x10]
       mov      bword ptr [rbp-0x58], rax
       mov      rax, bword ptr [rbp-0x58]
       movzx    rax, byte  ptr [rax+0x08]
       test     eax, eax
       je       G_M000_IG13
       mov      rax, bword ptr [rbp-0x58]
       mov      rax, gword ptr [rax]
       cmp      rax, gword ptr [rbp-0x38]
       jne      SHORT G_M000_IG09
       cmp      dword ptr [rbp-0x48], 1
       jne      SHORT G_M000_IG10
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      gword ptr [rbp-0x88], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0xB0], rax
       mov      rcx, gword ptr [rbp-0xB0]
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [rbp-0x88]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyFunction:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       nop      
 
G_M000_IG08:
       add      rsp, 192
       pop      rbp
       ret      
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       cmp      dword ptr [rbp-0x48], 3
       jne      SHORT G_M000_IG12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      gword ptr [rbp-0x80], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0xB8], rax
       mov      rcx, gword ptr [rbp-0xB8]
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [rbp-0x80]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       nop      
 
G_M000_IG11:
       add      rsp, 192
       pop      rbp
       ret      
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG17
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x4C]
       inc      eax
       mov      dword ptr [rbp-0x4C], eax
 
G_M000_IG14:
       mov      eax, dword ptr [rbp-0xA0]
       dec      eax
       mov      dword ptr [rbp-0xA0], eax
       cmp      dword ptr [rbp-0xA0], 0
       jg       SHORT G_M000_IG16
 
G_M000_IG15:
       lea      rdi, [rbp-0xA0]
       mov      esi, 137
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG16:
       cmp      dword ptr [rbp-0x4C], 4
       jl       G_M000_IG07
 
G_M000_IG17:
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x90], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0xC0], rax
       mov      r8, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0x90]
       mov      edi, dword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x98], rax
       cmp      dword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       mov      esi, dword ptr [rbp-0x2C]
       call     [RyuPy.Runtime.PyPic:NextVictim(byte[],int):int]
       mov      dword ptr [rbp-0x5C], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       mov      byte  ptr [rbp-0x68], 1
       mov      eax, dword ptr [rbp-0x48]
       mov      byte  ptr [rbp-0x67], al
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [rbp-0x5C]
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG20
       mov      edx, ecx
       imul     rdx, rdx, 16
       lea      rax, bword ptr [rax+rdx+0x10]
       mov      rdi, rax
       lea      rsi, bword ptr [rbp-0x70]
       call     CORINFO_HELP_ASSIGN_BYREF
       movsq    
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x98]
 
G_M000_IG19:
       add      rsp, 192
       pop      rbp
       ret      
 
G_M000_IG20:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 809

; Assembly listing for method RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 384
       lea      rbp, [rsp+0x180]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x180], zmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x50], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x120], 0x3E8
       lea      rdi, [rbp-0xF8]
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x50], rax
       mov      rax, gword ptr [rbp-0xF0]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x60], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0xD0], rax
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x68], rax
       cmp      gword ptr [rbp-0x68], 0
       jne      G_M000_IG06
       jmp      G_M000_IG04
 
G_M000_IG03:
       mov      rdi, gword ptr [rbp-0x68]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG10
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG11
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG12
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG13
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG14
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG04:
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, gword ptr [rbp-0x110]
       mov      rsi, 0xD1FFAB1E
       call     CORINFO_HELP_CLASSPROFILE32
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x128], rax
       mov      rsi, gword ptr [rbp-0x128]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFINTERFACE]
       mov      gword ptr [rbp-0x78], rax
       cmp      gword ptr [rbp-0x78], 0
       jne      G_M000_IG16
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x80], rax
       cmp      gword ptr [rbp-0x80], 0
       jne      G_M000_IG17
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x88], rax
       cmp      gword ptr [rbp-0x88], 0
       jne      G_M000_IG18
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x90], rax
       cmp      gword ptr [rbp-0x90], 0
       jne      G_M000_IG19
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x98], rax
       cmp      gword ptr [rbp-0x98], 0
       jne      G_M000_IG20
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xA0], rax
       cmp      gword ptr [rbp-0xA0], 0
       jne      G_M000_IG21
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xA8], rax
       cmp      gword ptr [rbp-0xA8], 0
 
G_M000_IG05:
       jne      G_M000_IG22
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xB0], rax
       cmp      gword ptr [rbp-0xB0], 0
       jne      G_M000_IG23
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xB8], rax
       cmp      gword ptr [rbp-0xB8], 0
       jne      G_M000_IG24
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xC0], rax
       cmp      gword ptr [rbp-0xC0], 0
       jne      G_M000_IG25
       jmp      G_M000_IG26
 
G_M000_IG06:
       mov      rdi, gword ptr [rbp-0x68]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:IsBuiltinConstructor(System.String):bool]
       test     eax, eax
       je       SHORT G_M000_IG07
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, gword ptr [rbp-0x68]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, gword ptr [rbp-0x130]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG07:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG09
 
G_M000_IG08:
       lea      rdi, [rbp-0x120]
       mov      esi, 365
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG09:
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x70]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, gword ptr [rbp-0x138]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyExceptionHierarchy:IsSubclass(System.String,System.String):bool]
       test     eax, eax
       je       G_M000_IG03
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x70]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, gword ptr [rbp-0x140]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyOps:ConstructWarning(System.String,System.Object[],System.String[]):RyuPy.Runtime.PyBaseException]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyEllipsis:Create(System.Object[],System.String[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       mov      edi, 0x1F43
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x148], rax
       mov      rsi, gword ptr [rbp-0x148]
       mov      rdi, gword ptr [rbp-0x100]
       call     [RyuPy.Runtime.PyNotImplementedError:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x100]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyNotImplemented:Create(System.Object[],System.String[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyRegex:ConstructError(System.Object[],System.String[],System.Object[]):RyuPy.Runtime.PyRegexError]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyJsonModule:ConstructError(System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyUnsupportedOperation:Construct(System.Object[],System.String[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0x118], rax
       mov      rdi, gword ptr [rbp-0x118]
       mov      rsi, 0xD1FFAB1E
       call     CORINFO_HELP_CLASSPROFILE32
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x150], rax
       mov      rdi, gword ptr [rbp-0x150]
       mov      rcx, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      r11, 0xD1FFAB1E
       call     [r11]RyuPy.Runtime.IPyNativeCallable:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x80]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyFunction:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x88]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBuiltinFunction:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x90]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyNativeType:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x98]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xA0]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBoundMethod:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xA8]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBuiltinMethod:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, gword ptr [rbp-0xB0]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyStaticMethod:get_Value():System.Object:this]
       mov      gword ptr [rbp-0x158], rax
       mov      rdi, gword ptr [rbp-0x158]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, gword ptr [rbp-0xB8]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClassMethod:get_Value():System.Object:this]
       mov      gword ptr [rbp-0x160], rax
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG25:
       mov      rdi, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:CallInstance(RyuPy.Runtime.PyInstance,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG27
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       mov      edi, 0x129E
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x168], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      gword ptr [rbp-0x170], rax
       mov      edi, 0x1FB5
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x178], rax
       mov      rdi, gword ptr [rbp-0x168]
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdx, gword ptr [rbp-0x178]
       call     [System.String:Concat(System.String,System.String,System.String):System.String]
       mov      gword ptr [rbp-0x180], rax
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, gword ptr [rbp-0x108]
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x108]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG28:
       add      rsp, 384
       pop      rbp
       ret      
 
; Total bytes of code 2315

