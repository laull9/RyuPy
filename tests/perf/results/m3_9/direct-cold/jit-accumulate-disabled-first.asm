; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 3 inlinees with PGO data; 12 single block inlinees; 17 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 152
       lea      rbp, [rsp+0xC0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x50]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x48]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r12, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       xor      eax, eax
       xor      ecx, ecx
       mov      dword ptr [rbp-0x38], ecx
       test     r15, r15
       je       G_M000_IG11
 
G_M000_IG03:
       mov      rdi, qword ptr [r15]
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      SHORT G_M000_IG10
 
G_M000_IG04:
       mov      esi, 1
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      edx, 1
       mov      rax, qword ptr [r15+0x08]
       mov      rcx, rax
       mov      eax, edx
       mov      edx, edi
 
G_M000_IG06:
       add      r13, 1
       jo       G_M000_IG55
       mov      qword ptr [rbp-0x30], rcx
       mov      dword ptr [rbp-0x38], edx
       mov      esi, eax
 
G_M000_IG07:
       cmp      r13, r12
       setl     dl
       movzx    rdx, dl
       test     edx, edx
       je       G_M000_IG30
 
G_M000_IG08:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG12
 
G_M000_IG09:
       mov      dword ptr [rbp-0x34], esi
       mov      rdx, 0xD1FFAB1E
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      qword ptr [rdx+0x08], r13
       mov      gword ptr [rbp-0xB8], rdx
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       xor      esi, esi
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG07
 
G_M000_IG11:
       xor      esi, esi
       mov      dword ptr [rbp-0x34], esi
       mov      qword ptr [rbp-0x30], rax
       mov      esi, dword ptr [rbp-0x34]
       jmp      SHORT G_M000_IG07
 
G_M000_IG12:
       test     rdi, rdi
       jl       G_M000_IG55
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG54
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      dword ptr [rbp-0x34], esi
       mov      gword ptr [rbp-0xB8], rdx
 
G_M000_IG13:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0xB8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      gword ptr [rbp-0xB0], rsi
       mov      eax, dword ptr [rbp-0x34]
       test     eax, eax
       je       SHORT G_M000_IG17
 
G_M000_IG14:
       test     rsi, rsi
       je       SHORT G_M000_IG17
 
G_M000_IG15:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rdx, qword ptr [rsi+0x08]
       mov      rcx, qword ptr [rbp-0x30]
       lea      rdi, [rcx+rdx]
       mov      r8, rcx
       xor      r8, rdi
       xor      rdx, rdi
       and      rdx, r8
       mov      qword ptr [rbp-0x30], rcx
       mov      rsi, gword ptr [rbp-0xB0]
       jl       SHORT G_M000_IG17
       mov      rcx, rdi
       mov      edx, 1
       jmp      G_M000_IG06
 
G_M000_IG17:
       cmp      dword ptr [rbp-0x38], 0
       je       G_M000_IG35
       mov      rcx, qword ptr [rbp-0x30]
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG18
       test     rdi, rdi
       jl       G_M000_IG55
       mov      r8, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      r8, 262
       jae      G_M000_IG54
       mov      r15, gword ptr [rdx+8*r8+0x10]
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdx, 0xD1FFAB1E
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [r15+0x08], rax
 
G_M000_IG19:
       test     r15, r15
       je       SHORT G_M000_IG25
 
G_M000_IG20:
       mov      rcx, qword ptr [r15]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       je       G_M000_IG36
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG47
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG25
 
G_M000_IG21:
       mov      rdi, r15
       mov      r8, gword ptr [rbp-0xB0]
       test     r8, r8
       je       SHORT G_M000_IG25
 
G_M000_IG22:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:
       mov      rsi, r8
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r15, rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rdx, gword ptr [rbp-0xB0]
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG26:
       xor      edx, edx
       xor      edi, edi
       test     r15, r15
       je       SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rax, qword ptr [r15]
       mov      rdx, 0xD1FFAB1E
       cmp      rax, rdx
       je       G_M000_IG05
 
G_M000_IG28:
       xor      edx, edx
       mov      eax, edx
       mov      edx, edi
       mov      rcx, qword ptr [rbp-0x30]
       jmp      G_M000_IG06
 
G_M000_IG29:
       xor      edx, edx
       mov      eax, edx
       mov      edx, edi
       mov      rcx, qword ptr [rbp-0x30]
       jmp      G_M000_IG06
 
G_M000_IG30:
       cmp      dword ptr [rbp-0x38], 0
       je       SHORT G_M000_IG33
 
G_M000_IG31:
       mov      rax, qword ptr [rbp-0x30]
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG32
       test     rdi, rdi
       jl       G_M000_IG55
       mov      r8, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      r8, 262
       jae      G_M000_IG54
       mov      r15, gword ptr [rdi+8*r8+0x10]
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:
       mov      rdx, 0xD1FFAB1E
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rbx, qword ptr [rbp-0x30]
       mov      qword ptr [r15+0x08], rbx
 
G_M000_IG33:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r15
 
G_M000_IG34:
       add      rsp, 152
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG35:
       jmp      G_M000_IG19
 
G_M000_IG36:
       mov      rsi, qword ptr [r15+0x08]
       mov      r8, gword ptr [rbp-0xB0]
       test     r8, r8
       jne      SHORT G_M000_IG38
 
G_M000_IG37:
       jmp      G_M000_IG25
 
G_M000_IG38:
       cmp      qword ptr [r8], rdx
       jne      G_M000_IG44
 
G_M000_IG39:
       mov      r15, qword ptr [r8+0x08]
       lea      rcx, [rsi+r15]
       mov      qword ptr [rbp-0x70], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      r8, r15
       xor      r8, rcx
       and      rdi, r8
       jl       SHORT G_M000_IG42
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG40
       test     rdi, rdi
       jl       G_M000_IG55
       mov      rdx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG54
       mov      r15, gword ptr [rdi+8*rdx+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, qword ptr [rbp-0x70]
       mov      qword ptr [r15+0x08], rdi
 
G_M000_IG41:
       jmp      SHORT G_M000_IG43
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0xC0], rax
       mov      ecx, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0xA4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xC0]
       mov      esi, dword ptr [rbp-0xA4]
       mov      rdx, gword ptr [rbp-0x90]
       mov      rcx, qword ptr [rbp-0x88]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r15, rax
 
G_M000_IG43:
       jmp      G_M000_IG26
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x98], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG26
 
G_M000_IG46:
       jmp      G_M000_IG25
 
G_M000_IG47:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       mov      r8, gword ptr [rbp-0xB0]
       test     r8, r8
       je       SHORT G_M000_IG53
 
G_M000_IG48:
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG50
 
G_M000_IG49:
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x58]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG26
 
G_M000_IG50:
       cmp      qword ptr [r8], rdx
       jne      SHORT G_M000_IG52
 
G_M000_IG51:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG26
 
G_M000_IG52:
       jmp      G_M000_IG25
 
G_M000_IG53:
       jmp      G_M000_IG25
 
G_M000_IG54:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG55:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1488

