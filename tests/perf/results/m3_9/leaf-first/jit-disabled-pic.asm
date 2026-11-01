; Assembly listing for method RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 9 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      r14d, edi
       mov      rbx, rsi
       mov      r15, rdx
 
G_M000_IG02:
       cmp      r14d, 0x1000
       jae      G_M000_IG24
 
G_M000_IG03:
       lea      r13d, [4*r14]
       mov      r12d, r13d
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyPic:KindOfCallable(System.Object):byte]
       mov      dword ptr [rbp-0x2C], eax
       mov      r8, 0xD1FFAB1E
       mov      r9, gword ptr [r8]
       mov      gword ptr [rbp-0x38], r9
       mov      r8d, r13d
       shl      r8, 4
       lea      r8, bword ptr [r9+r8+0x10]
       cmp      byte  ptr [r8+0x08], 0
       je       SHORT G_M000_IG05
 
G_M000_IG04:
       cmp      gword ptr [r8], rbx
       je       SHORT G_M000_IG12
 
G_M000_IG05:
       lea      r8d, [r12+0x01]
       shl      r8, 4
       lea      r8, bword ptr [r9+r8+0x10]
       cmp      byte  ptr [r8+0x08], 0
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      gword ptr [r8], rbx
       je       SHORT G_M000_IG12
 
G_M000_IG07:
       lea      r8d, [r12+0x02]
       shl      r8, 4
       lea      r8, bword ptr [r9+r8+0x10]
       cmp      byte  ptr [r8+0x08], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       cmp      gword ptr [r8], rbx
       je       SHORT G_M000_IG12
 
G_M000_IG09:
       add      r12d, 3
       mov      r8d, r12d
       shl      r8, 4
       lea      r8, bword ptr [r9+r8+0x10]
       cmp      byte  ptr [r8+0x08], 0
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       cmp      gword ptr [r8], rbx
       jne      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      r9, gword ptr [rbp-0x38]
 
G_M000_IG12:
       cmp      eax, 1
       je       G_M000_IG20
       cmp      eax, 3
       je       G_M000_IG16
 
G_M000_IG13:
       mov      r8, 0xD1FFAB1E
       mov      rdx, gword ptr [r8]
       mov      r8, rdx
       mov      rcx, rdx
       mov      edi, eax
       mov      rsi, rbx
       mov      rdx, r15
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      r15, rax
       mov      r12d, dword ptr [rbp-0x2C]
       test     r12d, r12d
       je       SHORT G_M000_IG14
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, r14d
       lea      rdi, bword ptr [rdi+rsi+0x10]
       movzx    rsi, byte  ptr [rdi]
       lea      eax, [rsi+0x01]
       mov      byte  ptr [rdi], al
       and      esi, 3
       add      esi, r13d
       mov      edi, esi
       shl      rdi, 4
       mov      r14, gword ptr [rbp-0x38]
       lea      r14, bword ptr [r14+rdi+0x10]
       mov      rdi, r14
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      byte  ptr [r14+0x08], 1
       mov      byte  ptr [r14+0x09], r12b
 
G_M000_IG14:
       mov      rax, r15
 
G_M000_IG15:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG16:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      G_M000_IG27
 
G_M000_IG18:
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, r15
       cmp      dword ptr [rdi], edi
 
G_M000_IG19:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
 
G_M000_IG20:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG22
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG26
 
G_M000_IG22:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      rdx, gword ptr [r8]
       mov      r8, rdx
       mov      rdx, r15
       mov      rdi, gword ptr [rax+0x08]
 
G_M000_IG23:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG24:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rcx, rdx
       mov      rdi, rbx
       mov      rsi, r15
 
G_M000_IG25:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
 
G_M000_IG26:
       mov      rsi, rbx
       call     CORINFO_HELP_CHKCASTCLASS
       int3     
 
G_M000_IG27:
       mov      rsi, rbx
       mov      rdi, rax
       call     CORINFO_HELP_CHKCASTCLASS
       int3     
 
; Total bytes of code 566

; Assembly listing for method RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 23 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x70]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
 
G_M000_IG02:
       lea      rdi, [rbp-0x40]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      r15, gword ptr [rbp-0x40]
       mov      r14, gword ptr [rbp-0x38]
       mov      r13, gword ptr [rbp-0x30]
       test     rbx, rbx
       je       G_M000_IG36
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG36
 
G_M000_IG04:
       mov      r12, rbx
       mov      rdi, gword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyOps:IsBuiltinConstructor(System.String):bool]
       test     eax, eax
       jne      G_M000_IG35
       mov      rdi, gword ptr [rbx+0x08]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyExceptionHierarchy:IsSubclass(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG34
       mov      r12, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       SHORT G_M000_IG08
 
G_M000_IG05:
       test     r12, r12
       je       SHORT G_M000_IG09
 
G_M000_IG06:
       cmp      dword ptr [r12+0x08], 8
       jne      SHORT G_M000_IG09
 
G_M000_IG07:
       vmovups  xmm0, xmmword ptr [r12+0x0C]
       vpxor    xmm0, xmm0, xmmword ptr [reloc @RWD00]
       vptest   xmm0, xmm0
       sete     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, r15
       mov      rsi, r14
       call     [RyuPy.Runtime.PyEllipsis:Create(System.Object[],System.String[]):System.Object]
       jmp      G_M000_IG64
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG67
 
G_M000_IG10:
       test     r12, r12
       je       SHORT G_M000_IG13
 
G_M000_IG11:
       cmp      dword ptr [r12+0x08], 14
       jne      SHORT G_M000_IG13
 
G_M000_IG12:
       vmovups  xmm0, xmmword ptr [r12+0x0C]
       vmovups  xmm1, xmmword ptr [r12+0x18]
       vpxor    xmm1, xmm1, xmmword ptr [reloc @RWD16]
       vpternlogq xmm0, xmm1, xmmword ptr [reloc @RWD32], -34
       vptest   xmm0, xmm0
       sete     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG67
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       SHORT G_M000_IG17
 
G_M000_IG14:
       test     r12, r12
       je       SHORT G_M000_IG18
 
G_M000_IG15:
       cmp      dword ptr [r12+0x08], 18
       jne      SHORT G_M000_IG18
 
G_M000_IG16:
       vmovups  ymm0, ymmword ptr [r12+0x0C]
       vmovups  ymm1, ymmword ptr [r12+0x10]
       vpxor    ymm1, ymm1, ymmword ptr [reloc @RWD64]
       vpternlogq ymm0, ymm1, ymmword ptr [reloc @RWD96], -34
       vptest   ymm0, ymm0
       sete     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       mov      rdi, r15
       mov      rsi, r14
       call     [RyuPy.Runtime.PyNotImplemented:Create(System.Object[],System.String[]):System.Object]
       jmp      G_M000_IG64
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       SHORT G_M000_IG22
 
G_M000_IG19:
       test     r12, r12
       je       SHORT G_M000_IG23
 
G_M000_IG20:
       cmp      dword ptr [r12+0x08], 5
       jne      SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       xor      rdi, qword ptr [r12+0x0C]
       movzx    rsi, word  ptr [r12+0x14]
       xor      esi, 114
       or       rdi, rsi
       jne      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      rdi, r15
       mov      rsi, r14
       mov      rdx, r13
       call     [RyuPy.Runtime.PyRegex:ConstructError(System.Object[],System.String[],System.Object[]):RyuPy.Runtime.PyRegexError]
       jmp      G_M000_IG64
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       SHORT G_M000_IG27
 
G_M000_IG24:
       test     r12, r12
       je       SHORT G_M000_IG29
 
G_M000_IG25:
       cmp      dword ptr [r12+0x08], 15
       jne      SHORT G_M000_IG29
 
G_M000_IG26:
       vmovups  xmm0, xmmword ptr [r12+0x0C]
       vmovups  xmm1, xmmword ptr [r12+0x1A]
       vpxor    xmm1, xmm1, xmmword ptr [reloc @RWD128]
       vpternlogq xmm0, xmm1, xmmword ptr [reloc @RWD144], -34
       vptest   xmm0, xmm0
       sete     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG29
 
G_M000_IG27:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG68
 
G_M000_IG28:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, gword ptr [rsi+0x18]
       mov      rdx, r15
       mov      rcx, r14
       mov      r8, r13
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
       jmp      G_M000_IG64
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       SHORT G_M000_IG33
 
G_M000_IG30:
       test     r12, r12
       je       G_M000_IG66
 
G_M000_IG31:
       cmp      dword ptr [r12+0x08], 20
       jne      G_M000_IG66
 
G_M000_IG32:
       vmovups  ymm0, ymmword ptr [r12+0x0C]
       vmovups  ymm1, ymmword ptr [r12+0x14]
       vpxor    ymm1, ymm1, ymmword ptr [reloc @RWD160]
       vpternlogq ymm0, ymm1, ymmword ptr [reloc @RWD192], -34
       vptest   ymm0, ymm0
       sete     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG66
 
G_M000_IG33:
       mov      rdi, r15
       mov      rsi, r14
       call     [RyuPy.Runtime.PyUnsupportedOperation:Construct(System.Object[],System.String[]):System.Object]
       jmp      G_M000_IG64
 
G_M000_IG34:
       mov      rdi, gword ptr [r12+0x08]
       mov      rsi, r15
       mov      rdx, r14
       call     [RyuPy.Runtime.PyOps:ConstructWarning(System.String,System.Object[],System.String[]):RyuPy.Runtime.PyBaseException]
       jmp      G_M000_IG64
 
G_M000_IG35:
       mov      rbx, gword ptr [r12+0x08]
       lea      rdi, [rbp-0x58]
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       jmp      G_M000_IG64
 
G_M000_IG36:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFINTERFACE
       test     rax, rax
       je       SHORT G_M000_IG37
       mov      rdi, rax
       mov      rcx, r13
       mov      rsi, r15
       mov      rdx, r14
       mov      r11, 0xD1FFAB1E
       call     [r11]RyuPy.Runtime.IPyNativeCallable:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this
       jmp      G_M000_IG64
 
G_M000_IG37:
       test     rbx, rbx
       je       SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rsi, rbx
       mov      rax, gword ptr [rsi+0x18]
       mov      rdx, r15
       mov      rcx, r14
       mov      r8, r13
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
       jmp      G_M000_IG64
 
G_M000_IG40:
       test     rbx, rbx
       je       SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG43
 
G_M000_IG42:
       mov      rdi, rbx
       mov      rbx, gword ptr [rdi+0x08]
       lea      rdi, [rbp-0x70]
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdx, gword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       jmp      G_M000_IG64
 
G_M000_IG43:
       test     rbx, rbx
       je       SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       mov      rdi, rbx
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyNativeType:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG64
 
G_M000_IG46:
       test     rbx, rbx
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG49
 
G_M000_IG48:
       mov      rdi, rbx
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG64
 
G_M000_IG49:
       test     rbx, rbx
       je       SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG52
 
G_M000_IG51:
       mov      rdi, rbx
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyBoundMethod:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG64
 
G_M000_IG52:
       test     rbx, rbx
       je       SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG55
 
G_M000_IG54:
       mov      rdi, gword ptr [rbx+0x08]
       mov      rsi, gword ptr [rbx+0x10]
       mov      edx, dword ptr [r15+0x08]
       call     [RyuPy.Runtime.PyOps:ValidatePreparedArity(System.Object,System.String,int)]
       mov      rax, gword ptr [rbx+0x18]
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdx, gword ptr [rbx+0x10]
       mov      rcx, r15
       mov      r8, r14
       mov      r9, r13
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyBuiltinMethodCall:Invoke(System.Object,System.String,System.Object[],System.String[],System.Object[]):System.Object:this
       jmp      G_M000_IG64
 
G_M000_IG55:
       test     rbx, rbx
       je       SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG57:
       mov      rdi, rbx
       mov      rdi, gword ptr [rdi+0x08]
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       jmp      SHORT G_M000_IG64
 
G_M000_IG58:
       test     rbx, rbx
       je       SHORT G_M000_IG61
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG61
 
G_M000_IG60:
       mov      rdi, rbx
       mov      rdi, gword ptr [rdi+0x08]
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyOps:CallValue(System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       jmp      SHORT G_M000_IG64
 
G_M000_IG61:
       test     rbx, rbx
       je       SHORT G_M000_IG66
 
G_M000_IG62:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG66
 
G_M000_IG63:
       mov      rdi, rbx
       mov      rsi, r15
       mov      rdx, r14
       mov      rcx, r13
       call     [RyuPy.Runtime.PyOps:CallInstance(RyuPy.Runtime.PyInstance,System.Object[],System.String[],System.Object[]):System.Object]
 
G_M000_IG64:
       nop      
 
G_M000_IG65:
       vzeroupper 
       add      rsp, 72
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x129E
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      rbx, rax
       mov      edi, 0x1FB5
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rdi, r14
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String,System.String):System.String]
       mov      rsi, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x1F43
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyNotImplementedError:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG28
 
RWD00  	dq	0069006C006C0065h, 0073006900730070h
RWD16  	dq	0074006300690044h, 006100740065004Dh
RWD32  	dq	007000790054005Fh, 0069004400640065h
RWD48  	dd	00000000h, 00000000h, 00000000h, 00000000h
RWD64  	dq	0070006D00490074h, 0065006D0065006Ch, 006400650074006Eh, 0065007000790054h
RWD96  	dq	00490074006F004Eh, 0065006C0070006Dh, 0074006E0065006Dh, 0079005400640065h
RWD128 	dq	004500650064006Fh, 0072006F00720072h
RWD144 	dq	004E004F0053004Ah, 006F006300650044h
RWD160 	dq	0072006F00700070h, 004F006400650074h, 0061007200650070h, 006E006F00690074h
RWD192 	dq	00750073006E0055h, 0072006F00700070h, 004F006400650074h, 0061007200650070h

; Total bytes of code 1498

