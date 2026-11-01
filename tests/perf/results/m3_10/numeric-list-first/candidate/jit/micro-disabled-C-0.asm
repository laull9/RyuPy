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

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 33 single block inlinees; 58 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 744
       lea      rbp, [rsp+0x310]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x250], xmm8
       vmovdqa32 xmmword ptr [rbp-0x240], xmm8
       mov      rax, -384
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
       mov      rdi, r12
       mov      esi, 1
       call     [RyuPy.Runtime.PyList:.ctor(bool):this]
       mov      edi, 41
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x288], rax
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, gword ptr [rbp-0x288]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x40], rax
       jmp      G_M000_IG79
 
G_M000_IG03:
       mov      ecx, 1
       mov      gword ptr [rbp-0x278], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      dword ptr [rbp-0x8C], ecx
       mov      rdx, gword ptr [rbp-0x278]
 
G_M000_IG04:
       mov      rax, qword ptr [rbp-0x50]
       add      rax, 1
       jo       G_M000_IG163
       mov      gword ptr [rbp-0x278], rdx
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x58]
       mov      r8d, dword ptr [rbp-0x8C]
 
G_M000_IG05:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG76
 
G_M000_IG06:
       mov      qword ptr [rbp-0x50], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG10
 
G_M000_IG07:
       mov      dword ptr [rbp-0x8C], r8d
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rsi+0x08], rax
       mov      gword ptr [rbp-0x2E0], rsi
       jmp      SHORT G_M000_IG11
 
G_M000_IG08:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x278], rdx
       vmovsd   qword ptr [rbp-0x88], xmm0
       jmp      SHORT G_M000_IG05
 
G_M000_IG09:
       xor      r8d, r8d
       mov      dword ptr [rbp-0x8C], r8d
       mov      gword ptr [rbp-0x278], rdx
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      r8d, dword ptr [rbp-0x8C]
       jmp      G_M000_IG05
 
G_M000_IG10:
       test     rdi, rdi
       jl       G_M000_IG163
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG162
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x2E0], rsi
       mov      dword ptr [rbp-0x8C], r8d
 
G_M000_IG11:
       mov      edi, 44
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x8C], 0
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG13
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG13
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x98], xmm0
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       jmp      SHORT G_M000_IG18
 
G_M000_IG13:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2A0], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2A8], rax
       mov      ecx, dword ptr [rbp-0x8C]
       test     ecx, ecx
       je       SHORT G_M000_IG19
       mov      rdx, gword ptr [rbp-0x2A0]
       test     rdx, rdx
       je       SHORT G_M000_IG20
 
G_M000_IG14:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG20
 
G_M000_IG15:
       test     rax, rax
       je       SHORT G_M000_IG20
 
G_M000_IG16:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG20
 
G_M000_IG17:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      dword ptr [rbp-0x8C], ecx
       vmovsd   qword ptr [rbp-0x98], xmm0
       vmovaps  xmm0, xmm1
 
G_M000_IG18:
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm1, xmm0
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rbp-0x88], xmm0
       xor      rdx, rdx
       jmp      G_M000_IG04
 
G_M000_IG19:
       mov      rdx, gword ptr [rbp-0x2A0]
 
G_M000_IG20:
       mov      r8, gword ptr [rbp-0x278]
       test     r8, r8
       sete     sil
       movzx    rsi, sil
       test     ecx, esi
       mov      gword ptr [rbp-0x278], r8
       je       SHORT G_M000_IG21
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      r8, rax
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [r8+0x08], xmm0
       mov      gword ptr [rbp-0x278], r8
 
G_M000_IG21:
       mov      rcx, gword ptr [rbp-0x2A0]
       test     rcx, rcx
       je       G_M000_IG49
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG42
 
G_M000_IG23:
       mov      rsi, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x2A8]
       test     r8, r8
       jne      SHORT G_M000_IG25
 
G_M000_IG24:
       jmp      G_M000_IG49
 
G_M000_IG25:
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG39
 
G_M000_IG26:
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
       jne      SHORT G_M000_IG29
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG27
       test     rcx, rcx
       jl       G_M000_IG163
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG162
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG28
 
G_M000_IG27:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x118]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG28:
       jmp      G_M000_IG32
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x110], xmm1
       lea      rdi, [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x2F0], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x264], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       mov      rsi, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x130]
       mov      r9d, dword ptr [rbp-0x128]
       mov      rcx, gword ptr [rbp-0x2F0]
       test     rcx, rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG30:
       test     rax, rax
       jne      SHORT G_M000_IG33
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       movsxd   rsi, dword ptr [rbp-0x264]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x140]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x140]
       mov      esi, dword ptr [rbp-0x138]
 
G_M000_IG31:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG32:
       jmp      G_M000_IG50
 
G_M000_IG33:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG35:
       test     rax, rax
       je       SHORT G_M000_IG38
 
G_M000_IG36:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG37:
       mov      edx, dword ptr [rbp-0x264]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG31
 
G_M000_IG38:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG37
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
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
       jmp      G_M000_IG50
 
G_M000_IG41:
       jmp      G_M000_IG49
 
G_M000_IG42:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG49
 
G_M000_IG43:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rsi, gword ptr [rbp-0x2A8]
       test     rsi, rsi
       je       SHORT G_M000_IG49
 
G_M000_IG44:
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG46:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG48
 
G_M000_IG47:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x150]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG48:
 
G_M000_IG49:
       mov      rdx, gword ptr [rbp-0x2A8]
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG50:
       mov      rax, gword ptr [rbp-0x278]
       test     rax, rax
       je       G_M000_IG71
 
G_M000_IG51:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG61
 
G_M000_IG52:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG53:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG59
 
G_M000_IG54:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x170], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x178], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG57
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG55
       test     rax, rax
       jl       G_M000_IG163
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG162
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG56:
       jmp      G_M000_IG58
 
G_M000_IG57:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       lea      rdi, [rbp-0x188]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x188]
       mov      gword ptr [rbp-0x2F8], rax
       mov      ecx, dword ptr [rbp-0x180]
       mov      dword ptr [rbp-0x268], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x198], xmm0
       lea      rdi, [rbp-0x198]
       mov      rsi, qword ptr [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2F8]
       mov      esi, dword ptr [rbp-0x268]
       mov      rdx, gword ptr [rbp-0x198]
       mov      rcx, qword ptr [rbp-0x190]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG58:
       jmp      G_M000_IG72
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG71
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x168], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1A0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1A0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG72
 
G_M000_IG61:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG67
 
G_M000_IG62:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm0
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG63:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG65
 
G_M000_IG64:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x160], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x158]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x160]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG65:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG66:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1A8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG68:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG71
 
G_M000_IG69:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG72
 
G_M000_IG71:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG72:
       test     rax, rax
       je       SHORT G_M000_IG75
 
G_M000_IG73:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       je       G_M000_IG03
 
G_M000_IG74:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x8C], ecx
       mov      rdx, rax
       jmp      G_M000_IG04
 
G_M000_IG75:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x8C], ecx
       mov      rdx, rax
       jmp      G_M000_IG04
 
G_M000_IG76:
       mov      dword ptr [rbp-0x8C], r8d
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x278]
       test     rax, rax
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp-0x8C], edi
       je       G_M000_IG83
 
G_M000_IG77:
       mov      rdi, r12
       vmovsd   xmm0, qword ptr [rbp-0x88]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
 
G_M000_IG78:
       mov      rcx, qword ptr [rbp-0x38]
       add      rcx, 1
       jo       G_M000_IG163
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG79:
       mov      qword ptr [rbp-0x38], rcx
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG84
 
G_M000_IG80:
       mov      edi, 42
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x278], rax
       mov      edi, 43
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x290], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2D0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0xC8]
       mov      rsi, gword ptr [rbp-0x2D0]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0xC8]
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x298], rax
       mov      rdi, gword ptr [rbp-0x290]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x48], rax
       mov      rdi, gword ptr [rbp-0x298]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x278]
       test     rdx, rdx
       je       G_M000_IG09
 
G_M000_IG81:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG08
 
G_M000_IG82:
       mov      r8d, 1
       mov      gword ptr [rbp-0x278], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       jmp      G_M000_IG05
 
G_M000_IG83:
       mov      rcx, rax
       mov      rsi, r12
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG78
 
G_M000_IG84:
       mov      edi, 46
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG161
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       lea      rdi, bword ptr [r15+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [rbx+0x08]
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
       jmp      G_M000_IG154
 
G_M000_IG86:
       mov      ecx, 1
       mov      gword ptr [rbp-0x280], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       mov      dword ptr [rbp-0xA4], ecx
       mov      rdx, gword ptr [rbp-0x280]
 
G_M000_IG87:
       mov      rax, qword ptr [rbp-0x78]
       add      rax, 1
       jo       G_M000_IG163
       mov      gword ptr [rbp-0x280], rdx
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x80]
       mov      r8d, dword ptr [rbp-0xA4]
 
G_M000_IG88:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG151
 
G_M000_IG89:
       mov      qword ptr [rbp-0x78], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG93
 
G_M000_IG90:
       mov      dword ptr [rbp-0xA4], r8d
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rsi+0x08], rax
       mov      gword ptr [rbp-0x2E8], rsi
       jmp      SHORT G_M000_IG94
 
G_M000_IG91:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x280], rdx
       vmovsd   qword ptr [rbp-0xA0], xmm0
       jmp      SHORT G_M000_IG88
 
G_M000_IG92:
       xor      r8d, r8d
       mov      dword ptr [rbp-0xA4], r8d
       mov      gword ptr [rbp-0x280], rdx
       vmovsd   qword ptr [rbp-0xA0], xmm0
       mov      r8d, dword ptr [rbp-0xA4]
       jmp      G_M000_IG88
 
G_M000_IG93:
       test     rdi, rdi
       jl       G_M000_IG163
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG162
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x2E8], rsi
       mov      dword ptr [rbp-0xA4], r8d
 
G_M000_IG94:
       mov      edi, 51
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0xA4], 0
       je       SHORT G_M000_IG96
 
G_M000_IG95:
       mov      rdi, r12
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG96
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG96
       mov      rdi, r12
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       jmp      SHORT G_M000_IG101
 
G_M000_IG96:
       mov      rdi, r12
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2C8], rax
       mov      ecx, dword ptr [rbp-0xA4]
       test     ecx, ecx
       je       SHORT G_M000_IG102
       mov      rdx, gword ptr [rbp-0x2C0]
       test     rdx, rdx
       je       SHORT G_M000_IG103
 
G_M000_IG97:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG103
 
G_M000_IG98:
       test     rax, rax
       je       SHORT G_M000_IG103
 
G_M000_IG99:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG103
 
G_M000_IG100:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      dword ptr [rbp-0xA4], ecx
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vmovaps  xmm0, xmm1
 
G_M000_IG101:
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, xmm0
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       xor      rdx, rdx
       jmp      G_M000_IG87
 
G_M000_IG102:
       mov      rdx, gword ptr [rbp-0x2C0]
 
G_M000_IG103:
       mov      r8, gword ptr [rbp-0x280]
       test     r8, r8
       sete     sil
       movzx    rsi, sil
       test     ecx, esi
       mov      gword ptr [rbp-0x280], r8
       je       SHORT G_M000_IG104
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      r8, rax
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [r8+0x08], xmm0
       mov      gword ptr [rbp-0x280], r8
 
G_M000_IG104:
       mov      rcx, gword ptr [rbp-0x2C0]
       test     rcx, rcx
       je       G_M000_IG124
 
G_M000_IG105:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG117
 
G_M000_IG106:
       mov      rsi, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x2C8]
       test     r8, r8
       jne      SHORT G_M000_IG108
 
G_M000_IG107:
       jmp      G_M000_IG124
 
G_M000_IG108:
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG114
 
G_M000_IG109:
       mov      rcx, qword ptr [r8+0x08]
       mov      qword ptr [rbp-0x1C8], rcx
       lea      r8, [rbp-0x1E8]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0x1E8]
       mov      qword ptr [rbp-0x1E0], r8
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
       jne      SHORT G_M000_IG112
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG110
       test     rcx, rcx
       jl       G_M000_IG163
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG162
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x1E0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG111:
       jmp      G_M000_IG113
 
G_M000_IG112:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm1
       lea      rdi, [rbp-0x1D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1D8]
       mov      gword ptr [rbp-0x300], rax
       mov      ecx, dword ptr [rbp-0x1D0]
       mov      dword ptr [rbp-0x26C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1F8], xmm0
       lea      rdi, [rbp-0x1F8]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x300]
       mov      esi, dword ptr [rbp-0x26C]
       mov      rdx, gword ptr [rbp-0x1F8]
       mov      rcx, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG113:
       jmp      G_M000_IG125
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG116
 
G_M000_IG115:
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x1C0], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x200], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG125
 
G_M000_IG116:
       jmp      G_M000_IG124
 
G_M000_IG117:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG124
 
G_M000_IG118:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm1
       mov      rsi, gword ptr [rbp-0x2C8]
       test     rsi, rsi
       je       SHORT G_M000_IG124
 
G_M000_IG119:
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG121
 
G_M000_IG120:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG125
 
G_M000_IG121:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG123
 
G_M000_IG122:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x208]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG125
 
G_M000_IG123:
 
G_M000_IG124:
       mov      rdx, gword ptr [rbp-0x2C8]
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG125:
       mov      rax, gword ptr [rbp-0x280]
       test     rax, rax
       je       G_M000_IG146
 
G_M000_IG126:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG136
 
G_M000_IG127:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG146
 
G_M000_IG128:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG134
 
G_M000_IG129:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x228], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x230], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG132
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG130
       test     rax, rax
       jl       G_M000_IG163
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG162
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG131
 
G_M000_IG130:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x230]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG131:
       jmp      G_M000_IG133
 
G_M000_IG132:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       lea      rdi, [rbp-0x240]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x240]
       mov      gword ptr [rbp-0x308], rax
       mov      ecx, dword ptr [rbp-0x238]
       mov      dword ptr [rbp-0x270], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
       lea      rdi, [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x308]
       mov      esi, dword ptr [rbp-0x270]
       mov      rdx, gword ptr [rbp-0x250]
       mov      rcx, qword ptr [rbp-0x248]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG133:
       jmp      G_M000_IG147
 
G_M000_IG134:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG146
 
G_M000_IG135:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x220], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x258], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x258]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG147
 
G_M000_IG136:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG142
 
G_M000_IG137:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm0
       test     rdx, rdx
       je       G_M000_IG146
 
G_M000_IG138:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG140
 
G_M000_IG139:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x210]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG147
 
G_M000_IG140:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG146
 
G_M000_IG141:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x260], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x260]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG147
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG146
 
G_M000_IG143:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG146
 
G_M000_IG144:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG146
 
G_M000_IG145:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG147
 
G_M000_IG146:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG147:
       test     rax, rax
       je       SHORT G_M000_IG150
 
G_M000_IG148:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       je       G_M000_IG86
 
G_M000_IG149:
       xor      ecx, ecx
       mov      dword ptr [rbp-0xA4], ecx
       mov      rdx, rax
       jmp      G_M000_IG87
 
G_M000_IG150:
       xor      ecx, ecx
       mov      dword ptr [rbp-0xA4], ecx
       mov      rdx, rax
       jmp      G_M000_IG87
 
G_M000_IG151:
       mov      dword ptr [rbp-0xA4], r8d
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x280]
       test     rax, rax
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp-0xA4], edi
       je       G_M000_IG158
 
G_M000_IG152:
       mov      rdi, rbx
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
 
G_M000_IG153:
       mov      rax, qword ptr [rbp-0x68]
       add      rax, 1
       jo       G_M000_IG163
 
G_M000_IG154:
       mov      qword ptr [rbp-0x68], rax
       cmp      rax, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG159
 
G_M000_IG155:
       mov      edi, 49
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x280], rax
       mov      edi, 50
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x2B0], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2D8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0x2D8]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0xE0]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x2B8], rax
       mov      rdi, gword ptr [rbp-0x2B0]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x2B8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x280]
       test     rdx, rdx
       je       G_M000_IG92
 
G_M000_IG156:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG91
 
G_M000_IG157:
       mov      r8d, 1
       mov      gword ptr [rbp-0x280], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       jmp      G_M000_IG88
 
G_M000_IG158:
       mov      rcx, rax
       mov      rsi, rbx
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG153
 
G_M000_IG159:
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
 
G_M000_IG160:
       add      rsp, 744
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG161:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG85
 
G_M000_IG162:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG163:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 5139

