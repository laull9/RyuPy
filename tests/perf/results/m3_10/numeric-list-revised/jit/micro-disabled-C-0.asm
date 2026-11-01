; Assembly listing for method RyuPy.Generated.Module:m0_construct_refs(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r13, rax
       lea      rdi, bword ptr [r13+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r12
       mov      rsi, r13
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, rbx
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rbx, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG03:
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 206

; Assembly listing for method RyuPy.Generated.Module:m0_append_refs(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 4 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r12
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [r13+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 27
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, rbx
       mov      rsi, r13
       mov      edi, 5
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       add      r12, 1
       jo       SHORT G_M000_IG06
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      SHORT G_M000_IG03
 
G_M000_IG04:
       mov      edi, 28
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG05:
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 295

; Assembly listing for method RyuPy.Generated.Module:m0_read_refs(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 3 inlinees with PGO data; 10 single block inlinees; 16 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 152
       lea      rbp, [rsp+0xC0]
       xor      eax, eax
       mov      qword ptr [rbp-0x88], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xB0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x48]
       mov      rsi, gword ptr [rbp-0xB0]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x48]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xA8], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, gword ptr [rbp-0xA8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       jmp      G_M000_IG33
 
G_M000_IG03:
       lea      rdi, [r12+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG05
 
G_M000_IG04:
       test     rdi, rdi
       jl       G_M000_IG37
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG36
       mov      rcx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0xB8], rcx
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      qword ptr [rcx+0x08], r12
       mov      gword ptr [rbp-0xB8], rcx
 
G_M000_IG06:
       mov      edi, 34
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0xB8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, r15
       call     [RyuPy.Runtime.PyOps:Is(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
 
G_M000_IG09:
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG32
 
G_M000_IG10:
       mov      edi, 35
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     r13, r13
       je       G_M000_IG31
 
G_M000_IG11:
       mov      rax, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG21
 
G_M000_IG12:
       mov      rsi, qword ptr [r13+0x08]
       test     rdx, rdx
       je       G_M000_IG31
 
G_M000_IG13:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG19
 
G_M000_IG14:
       mov      r13, qword ptr [rdx+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0x68], rax
       mov      rcx, rsi
       xor      rcx, rax
       mov      rdx, r13
       xor      rdx, rax
       and      rcx, rdx
       jl       SHORT G_M000_IG17
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG15
       test     rcx, rcx
       jl       G_M000_IG37
       mov      rdi, rcx
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG36
       mov      r13, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rdi, qword ptr [rbp-0x68]
       mov      qword ptr [r13+0x08], rdi
 
G_M000_IG16:
       jmp      SHORT G_M000_IG18
 
G_M000_IG17:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0xC0], rax
       mov      ecx, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0x9C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xC0]
       mov      esi, dword ptr [rbp-0x9C]
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, qword ptr [rbp-0x80]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG18:
       jmp      G_M000_IG32
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG31
 
G_M000_IG20:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x90], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG32
 
G_M000_IG21:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG27
 
G_M000_IG22:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       test     rdx, rdx
       je       G_M000_IG31
 
G_M000_IG23:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG25
 
G_M000_IG24:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG25:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG31
 
G_M000_IG26:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG31
 
G_M000_IG28:
       mov      rdi, r13
       test     rdx, rdx
       je       SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG31
 
G_M000_IG30:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG32:
       add      r12, 1
       jo       SHORT G_M000_IG37
       mov      rax, qword ptr [rbp-0x30]
 
G_M000_IG33:
       cmp      r12, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG34:
       mov      edi, 36
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG35:
       add      rsp, 152
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG36:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG37:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1107

; Assembly listing for method RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 7 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     rbx
       push     rax
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x20], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       test     rbx, rbx
       je       SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      rdi, rbx
       movzx    rsi, byte  ptr [rbx+0x74]
       test     esi, esi
       je       G_M000_IG57
       mov      rsi, r15
       call     [RyuPy.Runtime.PyEnum:GetItem(RyuPy.Runtime.PyClass,System.Object):System.Object]
       jmp      G_M000_IG58
 
G_M000_IG05:
       test     rbx, rbx
       je       G_M000_IG28
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG28
 
G_M000_IG07:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbx+0x08]
       mov      rax, 0xD1FFAB1E
       cmp      rsi, rax
       je       G_M000_IG27
 
G_M000_IG08:
       test     rsi, rsi
       je       SHORT G_M000_IG11
 
G_M000_IG09:
       cmp      dword ptr [rsi+0x08], 4
       jne      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi+0x0C], rax
       je       G_M000_IG27
 
G_M000_IG11:
       mov      rax, 0xD1FFAB1E
       cmp      rsi, rax
       je       G_M000_IG27
 
G_M000_IG12:
       test     rsi, rsi
       je       SHORT G_M000_IG15
 
G_M000_IG13:
       cmp      dword ptr [rsi+0x08], 4
       jne      SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi+0x0C], rax
       je       G_M000_IG27
 
G_M000_IG15:
       mov      rax, 0xD1FFAB1E
       cmp      rsi, rax
       je       G_M000_IG27
 
G_M000_IG16:
       test     rsi, rsi
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       cmp      dword ptr [rsi+0x08], 5
       jne      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rax, 0xD1FFAB1E
       xor      rax, qword ptr [rsi+0x0C]
       movzx    rcx, word  ptr [rsi+0x14]
       xor      ecx, 101
       or       rax, rcx
       je       G_M000_IG27
 
G_M000_IG19:
       mov      rax, 0xD1FFAB1E
       cmp      rsi, rax
       je       SHORT G_M000_IG27
 
G_M000_IG20:
       test     rsi, rsi
       je       SHORT G_M000_IG23
 
G_M000_IG21:
       cmp      dword ptr [rsi+0x08], 3
       jne      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      eax, dword ptr [rsi+0x0C]
       xor      eax, 0xD1FFAB1E
       movzx    rcx, word  ptr [rsi+0x10]
       xor      ecx, 116
       or       eax, ecx
       je       SHORT G_M000_IG27
 
G_M000_IG23:
       mov      rax, 0xD1FFAB1E
       cmp      rsi, rax
       je       SHORT G_M000_IG27
 
G_M000_IG24:
       test     rsi, rsi
       je       G_M000_IG57
 
G_M000_IG25:
       cmp      dword ptr [rsi+0x08], 9
       jne      G_M000_IG57
 
G_M000_IG26:
       vmovups  xmm0, xmmword ptr [rsi+0x0C]
       vmovups  xmm1, xmmword ptr [rsi+0x0E]
       vpxor    xmm1, xmm1, xmmword ptr [reloc @RWD00]
       vpternlogq xmm0, xmm1, xmmword ptr [reloc @RWD16], -34
       vptest   xmm0, xmm0
       sete     sil
       movzx    rsi, sil
       test     esi, esi
       je       G_M000_IG57
 
G_M000_IG27:
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       jmp      G_M000_IG58
 
G_M000_IG28:
       test     rbx, rbx
       je       SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG31
 
G_M000_IG30:
       mov      r14, rbx
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyAbc:IsAbc(RyuPy.Runtime.PyNativeType):bool]
       test     eax, eax
       je       G_M000_IG57
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       jmp      G_M000_IG58
 
G_M000_IG31:
       test     rbx, rbx
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG34
 
G_M000_IG33:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this]
       jmp      G_M000_IG58
 
G_M000_IG34:
       test     rbx, rbx
       je       SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG37
 
G_M000_IG36:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTuple:GetItem(System.Object):System.Object:this]
       jmp      G_M000_IG58
 
G_M000_IG37:
       test     rbx, rbx
       je       SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyDict:Get(System.Object):System.Object:this]
       jmp      G_M000_IG58
 
G_M000_IG40:
       test     rbx, rbx
       je       SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG43
 
G_M000_IG42:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyOps:GetString(System.String,System.Object):System.Object]
       jmp      G_M000_IG58
 
G_M000_IG43:
       test     rbx, rbx
       je       SHORT G_M000_IG50
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG50
 
G_M000_IG45:
       test     r15, r15
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG48
 
G_M000_IG47:
       mov      rsi, r15
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyBytes:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyBytes:this]
       jmp      SHORT G_M000_IG49
 
G_M000_IG48:
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyBytes:GetItem(long):System.Object:this]
 
G_M000_IG49:
       jmp      G_M000_IG58
 
G_M000_IG50:
       test     rbx, rbx
       je       SHORT G_M000_IG57
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG57
 
G_M000_IG52:
       test     r15, r15
       je       SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG55
 
G_M000_IG54:
       mov      rsi, r15
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyByteArray:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyByteArray:this]
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyByteArray:GetItem(long):System.Object:this]
 
G_M000_IG56:
       jmp      SHORT G_M000_IG58
 
G_M000_IG57:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyObject:MayHaveMethods(System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG59
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       lea      rdi, bword ptr [r14+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       lea      rcx, [rbp-0x20]
       mov      rdi, rbx
       mov      rdx, r14
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:TryMethod(System.Object,System.String,System.Object[],byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG60
       mov      rax, gword ptr [rbp-0x20]
 
G_M000_IG58:
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG59:
       xor      rdi, rdi
       mov      gword ptr [rbp-0x20], rdi
 
G_M000_IG60:
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
       mov      edi, 0x3930
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
 
RWD00  	dq	0065007A006F0072h, 007400650073006Eh
RWD16  	dq	007A006F00720066h, 00650073006E0065h

; Total bytes of code 1004

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 1 inlinees with PGO data; 1 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     rbx
       lea      rbp, [rsp+0x10]
       mov      rbx, rdi
 
G_M000_IG02:
       test     rsi, rsi
       je       SHORT G_M000_IG05
 
G_M000_IG03:
       mov      r15, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyList:this]
       jmp      G_M000_IG18
 
G_M000_IG05:
       test     rsi, rsi
       je       SHORT G_M000_IG12
 
G_M000_IG06:
       mov      r15, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       jne      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rsi, qword ptr [rsi+0x08]
       jmp      SHORT G_M000_IG13
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       movsxd   rsi, dword ptr [rsi+0x08]
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       jne      SHORT G_M000_IG12
 
G_M000_IG11:
       cmp      byte  ptr [rsi+0x08], 0
       setne    dil
       movzx    rdi, dil
       mov      esi, edi
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdi, rsi
       call     [RyuPy.Runtime.PyList:ToIndexSlow(System.Object):long]
       mov      rsi, rax
 
G_M000_IG13:
       mov      rax, gword ptr [rbx+0x08]
       test     rax, rax
       je       SHORT G_M000_IG17
       mov      ecx, dword ptr [rax+0x10]
       mov      edx, ecx
       test     rsi, rsi
       jge      SHORT G_M000_IG14
       movsxd   rdi, edx
       add      rdi, rsi
       jmp      SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rdi, rsi
 
G_M000_IG15:
       movsxd   rdx, edx
       cmp      rdi, rdx
       jae      SHORT G_M000_IG16
       cmp      edi, ecx
       jae      SHORT G_M000_IG20
       mov      rax, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG21
       mov      ecx, edi
       mov      rax, gword ptr [rax+8*rcx+0x10]
       jmp      SHORT G_M000_IG18
 
G_M000_IG16:
       call     [RyuPy.Runtime.PyList:ListIndexError():System.Object]
       jmp      SHORT G_M000_IG18
 
G_M000_IG17:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:ReadNumeric(long):System.Object:this]
 
G_M000_IG18:
       nop      
 
G_M000_IG19:
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG20:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG21:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 238

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 16 inlinees with PGO data; 42 single block inlinees; 92 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 984
       lea      rbp, [rsp+0x400]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -624
       vmovdqa32 xmmword ptr [rbp+rax-0xB0], xmm8
       vmovdqa32 xmmword ptr [rbp+rax-0xA0], xmm8
       vmovdqa32 xmmword ptr [rbp+rax-0x90], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      rbx, rdi
       mov      r15, rsi
       mov      r14, rdx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r13, rax
       mov      edi, 40
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3B8], rax
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG263
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x3B8]
       lea      rdi, bword ptr [rax+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, gword ptr [rbp-0x3B8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, 41
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x360], rax
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, gword ptr [rbp-0x360]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x40], rax
       jmp      G_M000_IG132
 
G_M000_IG04:
       mov      ecx, 1
       mov      gword ptr [rbp-0x348], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      dword ptr [rbp-0x8C], ecx
       mov      rdx, gword ptr [rbp-0x348]
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x50]
       add      rax, 1
       jo       G_M000_IG270
       mov      gword ptr [rbp-0x348], rdx
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x58]
       mov      r8d, dword ptr [rbp-0x8C]
 
G_M000_IG06:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG125
 
G_M000_IG07:
       mov      qword ptr [rbp-0x50], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG11
 
G_M000_IG08:
       mov      dword ptr [rbp-0x8C], r8d
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rsi+0x08], rax
       mov      gword ptr [rbp-0x3C0], rsi
       jmp      SHORT G_M000_IG12
 
G_M000_IG09:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x348], rdx
       vmovsd   qword ptr [rbp-0x88], xmm0
       jmp      SHORT G_M000_IG06
 
G_M000_IG10:
       xor      r8d, r8d
       mov      dword ptr [rbp-0x8C], r8d
       mov      gword ptr [rbp-0x348], rdx
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      r8d, dword ptr [rbp-0x8C]
       jmp      G_M000_IG06
 
G_M000_IG11:
       test     rdi, rdi
       jl       G_M000_IG270
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG269
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x3C0], rsi
       mov      dword ptr [rbp-0x8C], r8d
 
G_M000_IG12:
       mov      edi, 44
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x8C], 0
       je       G_M000_IG62
 
G_M000_IG13:
       test     rbx, rbx
       je       G_M000_IG62
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG62
 
G_M000_IG15:
       mov      rcx, gword ptr [rbp-0x3C0]
       test     rcx, rcx
       je       G_M000_IG61
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG60
 
G_M000_IG17:
       mov      gword ptr [rbp-0x3C0], rcx
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x1F8], rdx
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rdi, qword ptr [rbp-0x1F8]
       test     rdi, rdi
       jge      SHORT G_M000_IG18
       movsxd   rcx, eax
       add      rcx, rdi
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rcx, rdi
 
G_M000_IG19:
       movsxd   rdi, eax
       cmp      rcx, rdi
       jae      G_M000_IG62
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x208], xmm0
       mov      rdi, gword ptr [rbx+0x10]
       test     rdi, rdi
       jne      SHORT G_M000_IG22
       mov      rdi, gword ptr [rbx+0x08]
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      ecx, ecx
       mov      rdi, gword ptr [rdi+8*rcx+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rsi, 0xD1FFAB1E
       mov      rax, rsi
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG25
 
G_M000_IG21:
       xor      eax, eax
       jmp      SHORT G_M000_IG26
 
G_M000_IG22:
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, ecx
       shl      rax, 4
 
G_M000_IG23:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu  xmmword ptr [rbp-0x208], xmm0
 
G_M000_IG24:
       lea      rdi, [rbp-0x208]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG25:
       mov      eax, 1
 
G_M000_IG26:
       test     eax, eax
       je       G_M000_IG62
       test     r15, r15
       je       G_M000_IG62
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG62
 
G_M000_IG28:
       mov      rax, gword ptr [rbp-0x3C0]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1E0], rcx
       mov      rdi, r15
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rdi, qword ptr [rbp-0x1E0]
       test     rdi, rdi
       jge      SHORT G_M000_IG29
       movsxd   rcx, eax
       add      rcx, rdi
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:
       mov      rcx, rdi
 
G_M000_IG30:
       movsxd   rdi, eax
       cmp      rcx, rdi
       jae      G_M000_IG62
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       mov      rdi, gword ptr [r15+0x10]
       test     rdi, rdi
       jne      SHORT G_M000_IG33
       mov      rdi, gword ptr [r15+0x08]
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      ecx, ecx
       mov      rdi, gword ptr [rdi+8*rcx+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG36
 
G_M000_IG32:
       xor      eax, eax
       jmp      SHORT G_M000_IG37
 
G_M000_IG33:
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, ecx
       shl      rax, 4
 
G_M000_IG34:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
 
G_M000_IG35:
       lea      rdi, [rbp-0x1F0]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG36:
       mov      eax, 1
 
G_M000_IG37:
       test     eax, eax
       je       G_M000_IG62
       mov      rax, gword ptr [rbp-0x3C0]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1B0], rcx
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rcx, qword ptr [rbp-0x1B0]
       test     rcx, rcx
       jge      SHORT G_M000_IG38
       movsxd   rdx, eax
       add      rdx, rcx
       jmp      SHORT G_M000_IG39
 
G_M000_IG38:
       mov      rdx, rcx
 
G_M000_IG39:
       movsxd   rdi, eax
       cmp      rdx, rdi
       jae      G_M000_IG47
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       mov      rax, gword ptr [rbx+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG43
       mov      rdi, gword ptr [rbx+0x08]
       cmp      edx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       mov      rdi, gword ptr [rdi+8*rax+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG42
 
G_M000_IG40:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG42
 
G_M000_IG41:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG46
 
G_M000_IG42:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG48
 
G_M000_IG43:
       cmp      edx, dword ptr [rax+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rax+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       shl      rax, 4
 
G_M000_IG44:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG45:
       lea      rdi, [rbp-0x1C0]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG46:
       jmp      SHORT G_M000_IG48
 
G_M000_IG47:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG48:
       vmovsd   qword ptr [rbp-0x98], xmm0
       mov      rax, gword ptr [rbp-0x3C0]
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1C8], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rcx, qword ptr [rbp-0x1C8]
       test     rcx, rcx
       jge      SHORT G_M000_IG49
       movsxd   rdx, eax
       add      rdx, rcx
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:
       mov      rdx, rcx
 
G_M000_IG50:
       movsxd   rdi, eax
       cmp      rdx, rdi
       jae      G_M000_IG58
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
       mov      rax, gword ptr [r15+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG54
       mov      rdi, gword ptr [r15+0x08]
       cmp      edx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       mov      rdi, gword ptr [rdi+8*rax+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG53
 
G_M000_IG51:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG53
 
G_M000_IG52:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG57
 
G_M000_IG53:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG59
 
G_M000_IG54:
       cmp      edx, dword ptr [rax+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rax+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       shl      rax, 4
 
G_M000_IG55:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
 
G_M000_IG56:
       lea      rdi, [rbp-0x1D8]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG57:
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG59:
       jmp      G_M000_IG67
 
G_M000_IG60:
       mov      gword ptr [rbp-0x3C0], rcx
       jmp      SHORT G_M000_IG62
 
G_M000_IG61:
       mov      gword ptr [rbp-0x3C0], rcx
 
G_M000_IG62:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x3C0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x378], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x3C0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x380], rax
       mov      ecx, dword ptr [rbp-0x8C]
       test     ecx, ecx
       je       SHORT G_M000_IG68
       mov      rdx, gword ptr [rbp-0x378]
       test     rdx, rdx
       je       SHORT G_M000_IG69
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG69
 
G_M000_IG64:
       test     rax, rax
       je       SHORT G_M000_IG69
 
G_M000_IG65:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG69
 
G_M000_IG66:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      dword ptr [rbp-0x8C], ecx
       vmovsd   qword ptr [rbp-0x98], xmm0
       vmovaps  xmm0, xmm1
 
G_M000_IG67:
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm1, xmm0
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rbp-0x88], xmm0
       xor      rdx, rdx
       jmp      G_M000_IG05
 
G_M000_IG68:
       mov      rdx, gword ptr [rbp-0x378]
 
G_M000_IG69:
       mov      rsi, gword ptr [rbp-0x348]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       mov      gword ptr [rbp-0x348], rsi
       je       SHORT G_M000_IG70
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp-0x348], rsi
 
G_M000_IG70:
       mov      rcx, gword ptr [rbp-0x378]
       test     rcx, rcx
       je       G_M000_IG98
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG91
 
G_M000_IG72:
       mov      rsi, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x380]
       test     r8, r8
       jne      SHORT G_M000_IG74
 
G_M000_IG73:
       jmp      G_M000_IG98
 
G_M000_IG74:
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG88
 
G_M000_IG75:
       mov      rcx, qword ptr [r8+0x08]
       mov      qword ptr [rbp-0x100], rcx
       lea      r8, [rbp-0x120]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0x120]
       mov      qword ptr [rbp-0x118], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       mov      r9, rcx
       sar      r9, 63
       and      r9, rsi
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG78
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG76
       test     rcx, rcx
       jl       G_M000_IG270
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG269
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x118]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG77:
       jmp      G_M000_IG81
 
G_M000_IG78:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x110], xmm1
       lea      rdi, [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x3D8], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x324], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       mov      rsi, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x130]
       mov      r9d, dword ptr [rbp-0x128]
       mov      rcx, gword ptr [rbp-0x3D8]
       test     rcx, rcx
       jne      SHORT G_M000_IG83
 
G_M000_IG79:
       test     rax, rax
       jne      SHORT G_M000_IG82
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       movsxd   rsi, dword ptr [rbp-0x324]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x140]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x140]
       mov      esi, dword ptr [rbp-0x138]
 
G_M000_IG80:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG81:
       jmp      G_M000_IG99
 
G_M000_IG82:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG84
 
G_M000_IG83:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG84:
       test     rax, rax
       je       SHORT G_M000_IG87
 
G_M000_IG85:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG86:
       mov      edx, dword ptr [rbp-0x324]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG80
 
G_M000_IG87:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG86
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG90
 
G_M000_IG89:
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x148], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x148]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG99
 
G_M000_IG90:
       jmp      G_M000_IG98
 
G_M000_IG91:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG98
 
G_M000_IG92:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rsi, gword ptr [rbp-0x380]
       test     rsi, rsi
       je       SHORT G_M000_IG98
 
G_M000_IG93:
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG95
 
G_M000_IG94:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG99
 
G_M000_IG95:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG97
 
G_M000_IG96:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x150]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG99
 
G_M000_IG97:
 
G_M000_IG98:
       mov      rdx, gword ptr [rbp-0x380]
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG99:
       mov      rax, gword ptr [rbp-0x348]
       test     rax, rax
       je       G_M000_IG120
 
G_M000_IG100:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG110
 
G_M000_IG101:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG120
 
G_M000_IG102:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG108
 
G_M000_IG103:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x170], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x178], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG106
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG104
       test     rax, rax
       jl       G_M000_IG270
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG269
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG105:
       jmp      G_M000_IG107
 
G_M000_IG106:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       lea      rdi, [rbp-0x188]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x188]
       mov      gword ptr [rbp-0x3E0], rax
       mov      ecx, dword ptr [rbp-0x180]
       mov      dword ptr [rbp-0x328], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x198], xmm0
       lea      rdi, [rbp-0x198]
       mov      rsi, qword ptr [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3E0]
       mov      esi, dword ptr [rbp-0x328]
       mov      rdx, gword ptr [rbp-0x198]
       mov      rcx, qword ptr [rbp-0x190]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG107:
       jmp      G_M000_IG121
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG120
 
G_M000_IG109:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x168], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1A0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1A0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG121
 
G_M000_IG110:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG116
 
G_M000_IG111:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm0
       test     rdx, rdx
       je       G_M000_IG120
 
G_M000_IG112:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG114
 
G_M000_IG113:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x160], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x158]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x160]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG121
 
G_M000_IG114:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG120
 
G_M000_IG115:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1A8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG121
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG120
 
G_M000_IG117:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG120
 
G_M000_IG118:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG120
 
G_M000_IG119:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG121
 
G_M000_IG120:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG121:
       test     rax, rax
       je       SHORT G_M000_IG124
 
G_M000_IG122:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       mov      rcx, rsi
       cmp      rdi, rcx
       je       G_M000_IG04
 
G_M000_IG123:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x8C], ecx
       mov      rdx, rax
       jmp      G_M000_IG05
 
G_M000_IG124:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x8C], ecx
       mov      rdx, rax
       jmp      G_M000_IG05
 
G_M000_IG125:
       mov      dword ptr [rbp-0x8C], r8d
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x348]
       test     rax, rax
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp-0x8C], edi
       je       G_M000_IG137
 
G_M000_IG126:
       mov      rax, gword ptr [r12+0x10]
       mov      gword ptr [rbp-0x3C8], rax
       test     rax, rax
       jne      SHORT G_M000_IG129
       mov      rcx, 0xD1FFAB1E
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r12+0x08]
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG128
 
G_M000_IG127:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       mov      esi, esi
       mov      rdi, rax
       call     CORINFO_HELP_ARRADDR_ST
       jmp      SHORT G_M000_IG131
 
G_M000_IG128:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      SHORT G_M000_IG131
 
G_M000_IG129:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG264
 
G_M000_IG130:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x330], rcx
       mov      rax, gword ptr [rbp-0x3C8]
       inc      dword ptr [rax+0x14]
       mov      r8, gword ptr [rax+0x08]
       mov      r9d, dword ptr [rax+0x10]
       cmp      dword ptr [r8+0x08], r9d
       ja       G_M000_IG136
       mov      rdx, rcx
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
 
G_M000_IG131:
       mov      rcx, qword ptr [rbp-0x38]
       add      rcx, 1
       jo       G_M000_IG270
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG132:
       mov      qword ptr [rbp-0x38], rcx
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG138
 
G_M000_IG133:
       mov      edi, 42
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x348], rax
       mov      edi, 43
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x368], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x3A8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0xC8]
       mov      rsi, gword ptr [rbp-0x3A8]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0xC8]
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x370], rax
       mov      rdi, gword ptr [rbp-0x368]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x48], rax
       mov      rdi, gword ptr [rbp-0x370]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x348]
       test     rdx, rdx
       je       G_M000_IG10
 
G_M000_IG134:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG09
 
G_M000_IG135:
       mov      r8d, 1
       mov      gword ptr [rbp-0x348], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       jmp      G_M000_IG06
 
G_M000_IG136:
       lea      edi, [r9+0x01]
       mov      dword ptr [rax+0x10], edi
       mov      edi, r9d
       shl      rdi, 4
       lea      rax, bword ptr [r8+rdi+0x10]
       mov      bword ptr [rbp-0x3F8], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, bword ptr [rbp-0x3F8]
       mov      rsi, qword ptr [rbp-0x330]
       mov      qword ptr [rcx+0x08], rsi
       jmp      G_M000_IG131
 
G_M000_IG137:
       mov      rcx, rax
       mov      rsi, r12
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG131
 
G_M000_IG138:
       mov      edi, 46
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       lea      rdi, bword ptr [r15+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [rbx+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, 48
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      r15, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       mov      rax, qword ptr [rbp-0x60]
       jmp      G_M000_IG254
 
G_M000_IG139:
       mov      ecx, 1
       mov      gword ptr [rbp-0x358], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       mov      dword ptr [rbp-0xA4], ecx
       mov      rdx, gword ptr [rbp-0x358]
 
G_M000_IG140:
       mov      rax, qword ptr [rbp-0x78]
       add      rax, 1
       jo       G_M000_IG270
       mov      gword ptr [rbp-0x358], rdx
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x80]
       mov      r8d, dword ptr [rbp-0xA4]
 
G_M000_IG141:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG247
 
G_M000_IG142:
       mov      qword ptr [rbp-0x78], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG146
 
G_M000_IG143:
       mov      dword ptr [rbp-0xA4], r8d
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rsi+0x08], rax
       mov      rcx, rsi
       jmp      SHORT G_M000_IG147
 
G_M000_IG144:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x358], rdx
       vmovsd   qword ptr [rbp-0xA0], xmm0
       jmp      SHORT G_M000_IG141
 
G_M000_IG145:
       xor      r8d, r8d
       mov      dword ptr [rbp-0xA4], r8d
       mov      gword ptr [rbp-0x358], rdx
       vmovsd   qword ptr [rbp-0xA0], xmm0
       mov      r8d, dword ptr [rbp-0xA4]
       jmp      SHORT G_M000_IG141
 
G_M000_IG146:
       test     rdi, rdi
       jl       G_M000_IG270
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG269
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      dword ptr [rbp-0xA4], r8d
       mov      rcx, rsi
 
G_M000_IG147:
       mov      gword ptr [rbp-0x350], rcx
       mov      edi, 51
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0xA4], 0
       je       G_M000_IG192
 
G_M000_IG148:
       mov      rcx, gword ptr [rbp-0x350]
       test     rcx, rcx
       je       G_M000_IG192
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG191
 
G_M000_IG150:
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x310], rdx
       mov      rdi, r12
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rcx, qword ptr [rbp-0x310]
       test     rcx, rcx
       jge      SHORT G_M000_IG151
       movsxd   rdx, eax
       add      rdx, rcx
       jmp      SHORT G_M000_IG152
 
G_M000_IG151:
       mov      rdx, rcx
 
G_M000_IG152:
       movsxd   rsi, eax
       cmp      rdx, rsi
       jae      G_M000_IG192
       mov      esi, edx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x320], xmm0
       mov      rax, gword ptr [r12+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG153
       mov      rdi, gword ptr [r12+0x08]
       cmp      esi, dword ptr [rdi+0x10]
       jb       G_M000_IG265
       jmp      G_M000_IG262
 
G_M000_IG153:
       cmp      edx, dword ptr [rax+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rax+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       shl      rax, 4
 
G_M000_IG154:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu32 xmmword ptr [rbp-0x320], xmm0
 
G_M000_IG155:
       lea      rdi, [rbp-0x320]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG156:
       mov      eax, 1
 
G_M000_IG157:
       test     eax, eax
       je       G_M000_IG192
       test     r14, r14
       je       G_M000_IG192
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG192
 
G_M000_IG159:
       mov      rax, gword ptr [rbp-0x350]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x2F8], rcx
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rdi, qword ptr [rbp-0x2F8]
       test     rdi, rdi
       jge      SHORT G_M000_IG160
       movsxd   rcx, eax
       add      rcx, rdi
       jmp      SHORT G_M000_IG161
 
G_M000_IG160:
       mov      rcx, rdi
 
G_M000_IG161:
       movsxd   rdi, eax
       cmp      rcx, rdi
       jae      G_M000_IG192
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x308], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       jne      SHORT G_M000_IG164
       mov      rdi, gword ptr [r14+0x08]
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      ecx, ecx
       mov      rdi, gword ptr [rdi+8*rcx+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG163
 
G_M000_IG162:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG167
 
G_M000_IG163:
       xor      eax, eax
       jmp      SHORT G_M000_IG168
 
G_M000_IG164:
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, ecx
       shl      rax, 4
 
G_M000_IG165:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu  xmmword ptr [rbp-0x308], xmm0
 
G_M000_IG166:
       lea      rdi, [rbp-0x308]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG167:
       mov      eax, 1
 
G_M000_IG168:
       test     eax, eax
       je       G_M000_IG192
       mov      rax, gword ptr [rbp-0x350]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x2C8], rcx
       mov      rdi, r12
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rcx, qword ptr [rbp-0x2C8]
       test     rcx, rcx
       jge      SHORT G_M000_IG169
       movsxd   rdx, eax
       add      rdx, rcx
       jmp      SHORT G_M000_IG170
 
G_M000_IG169:
       mov      rdx, rcx
 
G_M000_IG170:
       movsxd   rdi, eax
       cmp      rdx, rdi
       jae      G_M000_IG178
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
       mov      rax, gword ptr [r12+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG174
       mov      rdi, gword ptr [r12+0x08]
       cmp      edx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       mov      rdi, gword ptr [rdi+8*rax+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG173
 
G_M000_IG171:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG173
 
G_M000_IG172:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG177
 
G_M000_IG173:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG179
 
G_M000_IG174:
       cmp      edx, dword ptr [rax+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rax+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       shl      rax, 4
 
G_M000_IG175:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
 
G_M000_IG176:
       lea      rdi, [rbp-0x2D8]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG177:
       jmp      SHORT G_M000_IG179
 
G_M000_IG178:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG179:
       vmovsd   qword ptr [rbp-0xB0], xmm0
       mov      rax, gword ptr [rbp-0x350]
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x2E0], rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rcx, qword ptr [rbp-0x2E0]
       test     rcx, rcx
       jge      SHORT G_M000_IG180
       movsxd   rdx, eax
       add      rdx, rcx
       jmp      SHORT G_M000_IG181
 
G_M000_IG180:
       mov      rdx, rcx
 
G_M000_IG181:
       movsxd   rdi, eax
       cmp      rdx, rdi
       jae      G_M000_IG189
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2F0], xmm0
       mov      rax, gword ptr [r14+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG185
       mov      rdi, gword ptr [r14+0x08]
       cmp      edx, dword ptr [rdi+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rdi+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       mov      rdi, gword ptr [rdi+8*rax+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG184
 
G_M000_IG182:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG184
 
G_M000_IG183:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG188
 
G_M000_IG184:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG190
 
G_M000_IG185:
       cmp      edx, dword ptr [rax+0x10]
       jae      G_M000_IG262
       mov      rdi, gword ptr [rax+0x08]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG269
       mov      eax, edx
       shl      rax, 4
 
G_M000_IG186:
       vmovdqu  xmm0, xmmword ptr [rdi+rax+0x10]
       vmovdqu32 xmmword ptr [rbp-0x2F0], xmm0
 
G_M000_IG187:
       lea      rdi, [rbp-0x2F0]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG188:
       jmp      SHORT G_M000_IG190
 
G_M000_IG189:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG190:
       jmp      SHORT G_M000_IG197
 
G_M000_IG191:
 
G_M000_IG192:
       mov      rdi, r12
       mov      rsi, gword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x398], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A0], rax
       mov      ecx, dword ptr [rbp-0xA4]
       test     ecx, ecx
       je       SHORT G_M000_IG198
       mov      rdx, gword ptr [rbp-0x398]
       test     rdx, rdx
       je       SHORT G_M000_IG199
 
G_M000_IG193:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG199
 
G_M000_IG194:
       test     rax, rax
       je       SHORT G_M000_IG199
 
G_M000_IG195:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG199
 
G_M000_IG196:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      dword ptr [rbp-0xA4], ecx
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vmovaps  xmm0, xmm1
 
G_M000_IG197:
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, xmm0
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       xor      rdx, rdx
       jmp      G_M000_IG140
 
G_M000_IG198:
       mov      rdx, gword ptr [rbp-0x398]
 
G_M000_IG199:
       mov      rsi, gword ptr [rbp-0x358]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       mov      gword ptr [rbp-0x358], rsi
       je       SHORT G_M000_IG200
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp-0x358], rsi
 
G_M000_IG200:
       mov      rcx, gword ptr [rbp-0x398]
       test     rcx, rcx
       je       G_M000_IG220
 
G_M000_IG201:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG213
 
G_M000_IG202:
       mov      rsi, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x3A0]
       test     r8, r8
       jne      SHORT G_M000_IG204
 
G_M000_IG203:
       jmp      G_M000_IG220
 
G_M000_IG204:
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG210
 
G_M000_IG205:
       mov      rcx, qword ptr [r8+0x08]
       mov      qword ptr [rbp-0x228], rcx
       lea      r8, [rbp-0x248]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0x248]
       mov      qword ptr [rbp-0x240], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       mov      r9, rcx
       sar      r9, 63
       and      r9, rsi
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG208
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG206
       test     rcx, rcx
       jl       G_M000_IG270
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG269
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG207
 
G_M000_IG206:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x240]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG207:
       jmp      G_M000_IG209
 
G_M000_IG208:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x238], xmm1
       lea      rdi, [rbp-0x238]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x238]
       mov      gword ptr [rbp-0x3E8], rax
       mov      ecx, dword ptr [rbp-0x230]
       mov      dword ptr [rbp-0x334], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
       lea      rdi, [rbp-0x258]
       mov      rsi, qword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3E8]
       mov      esi, dword ptr [rbp-0x334]
       mov      rdx, gword ptr [rbp-0x258]
       mov      rcx, qword ptr [rbp-0x250]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG209:
       jmp      G_M000_IG221
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG212
 
G_M000_IG211:
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x220], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x260], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x260]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG221
 
G_M000_IG212:
       jmp      G_M000_IG220
 
G_M000_IG213:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG220
 
G_M000_IG214:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       mov      rsi, gword ptr [rbp-0x3A0]
       test     rsi, rsi
       je       SHORT G_M000_IG220
 
G_M000_IG215:
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG217
 
G_M000_IG216:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x210]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG221
 
G_M000_IG217:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG219
 
G_M000_IG218:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x268], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x268]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG221
 
G_M000_IG219:
 
G_M000_IG220:
       mov      rdx, gword ptr [rbp-0x3A0]
       mov      rsi, rcx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG221:
       mov      rax, gword ptr [rbp-0x358]
       test     rax, rax
       je       G_M000_IG242
 
G_M000_IG222:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG232
 
G_M000_IG223:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG242
 
G_M000_IG224:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG230
 
G_M000_IG225:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x288], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x290], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG228
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG226
       test     rax, rax
       jl       G_M000_IG270
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG269
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG227
 
G_M000_IG226:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x290]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG227:
       jmp      G_M000_IG229
 
G_M000_IG228:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2A0], xmm0
       lea      rdi, [rbp-0x2A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x2A0]
       mov      gword ptr [rbp-0x3F0], rax
       mov      ecx, dword ptr [rbp-0x298]
       mov      dword ptr [rbp-0x338], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2B0], xmm0
       lea      rdi, [rbp-0x2B0]
       mov      rsi, qword ptr [rbp-0x288]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3F0]
       mov      esi, dword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x2B0]
       mov      rcx, qword ptr [rbp-0x2A8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG229:
       jmp      G_M000_IG243
 
G_M000_IG230:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG242
 
G_M000_IG231:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x2B8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2B8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x280]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG243
 
G_M000_IG232:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG238
 
G_M000_IG233:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x270], xmm0
       test     rdx, rdx
       je       G_M000_IG242
 
G_M000_IG234:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG236
 
G_M000_IG235:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x270]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG243
 
G_M000_IG236:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG242
 
G_M000_IG237:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x2C0], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2C0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x270]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG243
 
G_M000_IG238:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG242
 
G_M000_IG239:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG242
 
G_M000_IG240:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG243
 
G_M000_IG242:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG243:
       test     rax, rax
       je       SHORT G_M000_IG246
 
G_M000_IG244:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       mov      rcx, rsi
       cmp      rdi, rcx
       je       G_M000_IG139
 
G_M000_IG245:
       xor      ecx, ecx
       mov      dword ptr [rbp-0xA4], ecx
       mov      rdx, rax
       jmp      G_M000_IG140
 
G_M000_IG246:
       xor      ecx, ecx
       mov      dword ptr [rbp-0xA4], ecx
       mov      rdx, rax
       jmp      G_M000_IG140
 
G_M000_IG247:
       mov      dword ptr [rbp-0xA4], r8d
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x358]
       test     rax, rax
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp-0xA4], edi
       je       G_M000_IG259
 
G_M000_IG248:
       mov      rax, gword ptr [rbx+0x10]
       mov      gword ptr [rbp-0x3D0], rax
       test     rax, rax
       jne      SHORT G_M000_IG251
       mov      rcx, 0xD1FFAB1E
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [rbx+0x08]
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG250
 
G_M000_IG249:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       movsxd   rsi, esi
       mov      rdi, rax
       call     CORINFO_HELP_ARRADDR_ST
       jmp      SHORT G_M000_IG253
 
G_M000_IG250:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      SHORT G_M000_IG253
 
G_M000_IG251:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG268
 
G_M000_IG252:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, qword ptr [rbp-0xA0]
       mov      qword ptr [rbp-0x340], rcx
       mov      rax, gword ptr [rbp-0x3D0]
       inc      dword ptr [rax+0x14]
       mov      r8, gword ptr [rax+0x08]
       mov      r9d, dword ptr [rax+0x10]
       cmp      dword ptr [r8+0x08], r9d
       ja       G_M000_IG258
       mov      rdx, rcx
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
 
G_M000_IG253:
       mov      rax, qword ptr [rbp-0x68]
       add      rax, 1
       jo       G_M000_IG270
 
G_M000_IG254:
       mov      qword ptr [rbp-0x68], rax
       cmp      rax, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG260
 
G_M000_IG255:
       mov      edi, 49
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x358], rax
       mov      edi, 50
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x388], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x3B0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0x3B0]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0xE0]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x390], rax
       mov      rdi, gword ptr [rbp-0x388]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x390]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x358]
       test     rdx, rdx
       je       G_M000_IG145
 
G_M000_IG256:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG144
 
G_M000_IG257:
       mov      r8d, 1
       mov      gword ptr [rbp-0x358], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       jmp      G_M000_IG141
 
G_M000_IG258:
       lea      edi, [r9+0x01]
       mov      dword ptr [rax+0x10], edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG269
       mov      edi, r9d
       shl      rdi, 4
       lea      rax, bword ptr [r8+rdi+0x10]
       mov      bword ptr [rbp-0x400], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, bword ptr [rbp-0x400]
       mov      rsi, qword ptr [rbp-0x340]
       mov      qword ptr [rcx+0x08], rsi
       jmp      G_M000_IG253
 
G_M000_IG259:
       mov      rcx, rax
       mov      rsi, rbx
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG253
 
G_M000_IG260:
       mov      edi, 53
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG261:
       add      rsp, 984
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG262:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG263:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG03
 
G_M000_IG264:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG130
 
G_M000_IG265:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG269
       mov      esi, esi
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       test     rsi, rsi
       jne      SHORT G_M000_IG267
 
G_M000_IG266:
       xor      eax, eax
       jmp      G_M000_IG157
 
G_M000_IG267:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG266
       cmp      qword ptr [rsi], rdi
       je       G_M000_IG156
       call     [CORINFO_HELP_UNBOX]
       jmp      G_M000_IG156
 
G_M000_IG268:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG252
 
G_M000_IG269:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG270:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7450

