; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mul_add(System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 12 single block inlinees; 28 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 232
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0xE8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       mov      qword ptr [rbp-0x60], rax
       mov      r14, rdi
       mov      r15, rsi
       mov      rbx, rdx
       mov      r13, rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r12, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rax, qword ptr [rbp-0x30]
       jmp      G_M000_IG46
 
G_M000_IG03:
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x70]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG04:
       jmp      G_M000_IG08
 
G_M000_IG05:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x108], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xFC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x108]
       test     rcx, rcx
       jne      SHORT G_M000_IG10
 
G_M000_IG06:
       test     rax, rax
       jne      SHORT G_M000_IG09
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0xFC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG07:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG08:
       jmp      G_M000_IG24
 
G_M000_IG09:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG11:
       test     rax, rax
       je       SHORT G_M000_IG14
 
G_M000_IG12:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG13:
       mov      edx, dword ptr [rbp-0xFC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG07
 
G_M000_IG14:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG13
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG23
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xA0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG24
 
G_M000_IG17:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG23
 
G_M000_IG18:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG23
 
G_M000_IG19:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG23
 
G_M000_IG22:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      rdx, rbx
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG24:
       test     rsi, rsi
       je       G_M000_IG52
 
G_M000_IG25:
       mov      rax, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG35
 
G_M000_IG26:
       mov      rax, qword ptr [rsi+0x08]
       test     r13, r13
       je       G_M000_IG52
 
G_M000_IG27:
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG33
 
G_M000_IG28:
       mov      r13, qword ptr [r13+0x08]
       lea      rcx, [rax+r13]
       mov      qword ptr [rbp-0xC8], rcx
       mov      rdx, rax
       xor      rdx, rcx
       mov      rsi, r13
       xor      rsi, rcx
       and      rdx, rsi
       jl       SHORT G_M000_IG31
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG29
       test     rax, rax
       jl       G_M000_IG56
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG55
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rdi, qword ptr [rbp-0xC8]
       mov      qword ptr [r13+0x08], rdi
 
G_M000_IG30:
       jmp      G_M000_IG32
 
G_M000_IG31:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      gword ptr [rbp-0x110], rax
       mov      ecx, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0x100], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       lea      rdi, [rbp-0xE8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x110]
       mov      esi, dword ptr [rbp-0x100]
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, qword ptr [rbp-0xE0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG32:
       jmp      G_M000_IG45
 
G_M000_IG33:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG52
 
G_M000_IG34:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG45
 
G_M000_IG35:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG41
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       test     r13, r13
       je       G_M000_IG52
 
G_M000_IG37:
       cmp      qword ptr [r13], rcx
       jne      SHORT G_M000_IG39
 
G_M000_IG38:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG45
 
G_M000_IG39:
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG52
 
G_M000_IG40:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG45
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG52
 
G_M000_IG42:
       mov      rdi, rsi
       test     r13, r13
       je       G_M000_IG52
 
G_M000_IG43:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      G_M000_IG52
 
G_M000_IG44:
       mov      rsi, r13
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
 
G_M000_IG45:
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG56
 
G_M000_IG46:
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG53
 
G_M000_IG47:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r15, r15
       je       G_M000_IG23
 
G_M000_IG48:
       mov      rax, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG17
 
G_M000_IG49:
       mov      rsi, qword ptr [r15+0x08]
       test     rbx, rbx
       je       G_M000_IG23
 
G_M000_IG50:
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG15
 
G_M000_IG51:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x58], rax
       lea      rcx, [rbp-0x78]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x70], rcx
       mov      r8, rsi
       sar      r8, 63
       and      r8, rax
       sub      rdx, r8
       mov      r8, rax
       sar      r8, 63
       and      r8, rsi
       sub      rdx, r8
       mov      r8, rcx
       sar      r8, 63
       cmp      rdx, r8
       jne      G_M000_IG05
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       G_M000_IG03
       test     rax, rax
       jl       SHORT G_M000_IG56
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      SHORT G_M000_IG55
       mov      rsi, gword ptr [rax+8*rdi+0x10]
       jmp      G_M000_IG04
 
G_M000_IG52:
       mov      rdx, r13
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
       jmp      G_M000_IG45
 
G_M000_IG53:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r12
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG54:
       add      rsp, 232
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG55:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG56:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1533

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_norm(System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 12 inlinees with PGO data; 28 single block inlinees; 70 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 552
       lea      rbp, [rsp+0x250]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x1E0], xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r13, rdi
       mov      rbx, rsi
       mov      r15, rdx
       mov      r14, rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r12, rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x210], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rax, qword ptr [rbp-0x30]
       jmp      G_M000_IG100
 
G_M000_IG03:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x68]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG04:
       mov      rax, rdx
       jmp      G_M000_IG08
 
G_M000_IG05:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x228], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x1F4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      r9d, dword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0x228]
       test     rcx, rcx
       jne      SHORT G_M000_IG10
 
G_M000_IG06:
       test     rax, rax
       jne      SHORT G_M000_IG09
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       movsxd   rsi, dword ptr [rbp-0x1F4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x90]
       mov      esi, dword ptr [rbp-0x88]
 
G_M000_IG07:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG08:
       jmp      G_M000_IG18
 
G_M000_IG09:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG11:
       test     rax, rax
       je       SHORT G_M000_IG14
 
G_M000_IG12:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG13:
       mov      edx, dword ptr [rbp-0x1F4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG07
 
G_M000_IG14:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG13
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG17
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vmovsd   qword ptr [rbp-0x48], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG17:
       mov      rdx, rbx
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x218], rax
       mov      rax, gword ptr [rbp-0x218]
 
G_M000_IG18:
       test     r15, r15
       jne      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      gword ptr [rbp-0x218], rax
 
G_M000_IG20:
       mov      rdx, r15
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG37
 
G_M000_IG21:
       mov      rcx, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG35
 
G_M000_IG22:
       mov      rcx, qword ptr [r15+0x08]
       mov      rsi, rcx
       mov      qword ptr [rbp-0xA8], rcx
       lea      r8, [rbp-0xC8]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0xC8]
       mov      qword ptr [rbp-0xC0], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG25
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG23
       test     rcx, rcx
       jl       G_M000_IG108
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rdx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x218], rax
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      gword ptr [rbp-0x218], rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG24:
       jmp      G_M000_IG28
 
G_M000_IG25:
       mov      gword ptr [rbp-0x218], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x230], rax
       mov      ecx, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x1F8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      r9d, dword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x230]
       test     rcx, rcx
       jne      SHORT G_M000_IG30
 
G_M000_IG26:
       test     rax, rax
       jne      SHORT G_M000_IG29
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       movsxd   rsi, dword ptr [rbp-0x1F8]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xE8]
       mov      esi, dword ptr [rbp-0xE0]
 
G_M000_IG27:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG28:
       jmp      G_M000_IG37
 
G_M000_IG29:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG31:
       test     rax, rax
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG33:
       mov      edx, dword ptr [rbp-0x1F8]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG27
 
G_M000_IG34:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG33
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       mov      gword ptr [rbp-0x218], rax
       jne      G_M000_IG20
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       vmovsd   qword ptr [rbp-0xA0], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
 
G_M000_IG37:
       mov      rax, gword ptr [rbp-0x218]
       test     rax, rax
       je       G_M000_IG58
 
G_M000_IG38:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG48
 
G_M000_IG39:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG58
 
G_M000_IG40:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG46
 
G_M000_IG41:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x108], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x110], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG44
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG42
       test     rax, rax
       jl       G_M000_IG108
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG43
 
G_M000_IG42:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x110]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG43:
       jmp      G_M000_IG45
 
G_M000_IG44:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x120], xmm0
       lea      rdi, [rbp-0x120]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x120]
       mov      gword ptr [rbp-0x238], rax
       mov      ecx, dword ptr [rbp-0x118]
       mov      dword ptr [rbp-0x1FC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       mov      rsi, qword ptr [rbp-0x108]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x238]
       mov      esi, dword ptr [rbp-0x1FC]
       mov      rdx, gword ptr [rbp-0x130]
       mov      rcx, qword ptr [rbp-0x128]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG45:
       jmp      G_M000_IG59
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG58
 
G_M000_IG47:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x138], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x138]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG59
 
G_M000_IG48:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG54
 
G_M000_IG49:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       test     rdx, rdx
       je       G_M000_IG58
 
G_M000_IG50:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG52
 
G_M000_IG51:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG59
 
G_M000_IG52:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG53:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG59
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG55:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG58
 
G_M000_IG57:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x220], rax
       mov      rax, gword ptr [rbp-0x220]
 
G_M000_IG59:
       test     r14, r14
       jne      SHORT G_M000_IG62
 
G_M000_IG60:
       mov      gword ptr [rbp-0x220], rax
 
G_M000_IG61:
       mov      rdx, r14
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG78
 
G_M000_IG62:
       mov      rcx, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG76
 
G_M000_IG63:
       mov      rcx, qword ptr [r14+0x08]
       mov      rsi, rcx
       mov      qword ptr [rbp-0x158], rcx
       lea      r8, [rbp-0x178]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0x178]
       mov      qword ptr [rbp-0x170], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG66
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG64
       test     rcx, rcx
       jl       G_M000_IG108
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rdx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x220], rax
       jmp      SHORT G_M000_IG65
 
G_M000_IG64:
       mov      gword ptr [rbp-0x220], rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x170]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG65:
       jmp      G_M000_IG69
 
G_M000_IG66:
       mov      gword ptr [rbp-0x220], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x240], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x200], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       lea      rdi, [rbp-0x188]
       mov      rsi, qword ptr [rbp-0x158]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x188]
       mov      r9d, dword ptr [rbp-0x180]
       mov      rcx, gword ptr [rbp-0x240]
       test     rcx, rcx
       jne      SHORT G_M000_IG71
 
G_M000_IG67:
       test     rax, rax
       jne      SHORT G_M000_IG70
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x198], xmm0
       movsxd   rsi, dword ptr [rbp-0x200]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x198]
       mov      esi, dword ptr [rbp-0x190]
 
G_M000_IG68:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG69:
       jmp      G_M000_IG78
 
G_M000_IG70:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG72
 
G_M000_IG71:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG72:
       test     rax, rax
       je       SHORT G_M000_IG75
 
G_M000_IG73:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG74:
       mov      edx, dword ptr [rbp-0x200]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG68
 
G_M000_IG75:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG74
 
G_M000_IG76:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       mov      gword ptr [rbp-0x220], rax
       jne      G_M000_IG61
 
G_M000_IG77:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm0
       vmovsd   qword ptr [rbp-0x150], xmm0
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
 
G_M000_IG78:
       mov      rax, gword ptr [rbp-0x220]
       test     rax, rax
       je       G_M000_IG104
 
G_M000_IG79:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG89
 
G_M000_IG80:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG104
 
G_M000_IG81:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG87
 
G_M000_IG82:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1B8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1C0], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG85
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG83
       test     rax, rax
       jl       G_M000_IG108
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG84
 
G_M000_IG83:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1C0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG84:
       jmp      G_M000_IG86
 
G_M000_IG85:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
       lea      rdi, [rbp-0x1D0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1D0]
       mov      gword ptr [rbp-0x248], rax
       mov      ecx, dword ptr [rbp-0x1C8]
       mov      dword ptr [rbp-0x204], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x1B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x248]
       mov      esi, dword ptr [rbp-0x204]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG86:
       jmp      G_M000_IG99
 
G_M000_IG87:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG104
 
G_M000_IG88:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG99
 
G_M000_IG89:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG95
 
G_M000_IG90:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       test     rdx, rdx
       je       G_M000_IG104
 
G_M000_IG91:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG93
 
G_M000_IG92:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG99
 
G_M000_IG93:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG104
 
G_M000_IG94:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG99
 
G_M000_IG95:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG104
 
G_M000_IG96:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG104
 
G_M000_IG97:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG104
 
G_M000_IG98:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
 
G_M000_IG99:
       mov      gword ptr [rbp-0x210], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG108
       mov      rcx, rbx
       mov      rbx, r15
       mov      r15, r14
       mov      r14, rcx
 
G_M000_IG100:
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r13
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG105
 
G_M000_IG101:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     rbx, rbx
       je       G_M000_IG17
 
G_M000_IG102:
       mov      rax, qword ptr [rbx]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG15
 
G_M000_IG103:
       mov      rax, qword ptr [rbx+0x08]
       mov      rsi, rax
       mov      qword ptr [rbp-0x50], rax
       lea      rcx, [rbp-0x70]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x68], rcx
       mov      r8, rsi
       sar      r8, 63
       and      r8, rax
       sub      rdx, r8
       sub      rdx, r8
       mov      r8, rcx
       sar      r8, 63
       cmp      rdx, r8
       jne      G_M000_IG05
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       G_M000_IG03
       test     rax, rax
       jl       SHORT G_M000_IG108
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      SHORT G_M000_IG107
       mov      rdx, gword ptr [rax+8*rdi+0x10]
       jmp      G_M000_IG04
 
G_M000_IG104:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       jmp      G_M000_IG99
 
G_M000_IG105:
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r12
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x210]
 
G_M000_IG106:
       add      rsp, 552
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG107:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG108:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3083

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 19 single block inlinees; 43 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 248
       lea      rbp, [rsp+0x120]
       xor      eax, eax
       mov      qword ptr [rbp-0xE8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       mov      qword ptr [rbp-0x60], rax
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       jmp      G_M000_IG58
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      gword ptr [rbp-0x110], rax
 
G_M000_IG04:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x110]
       test     rax, rax
       je       SHORT G_M000_IG09
 
G_M000_IG05:
       mov      rdi, qword ptr [rax]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG09
 
G_M000_IG06:
       mov      rsi, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x30], rsi
       test     rdx, rdx
       je       SHORT G_M000_IG09
 
G_M000_IG07:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      r8, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x38], r8
       test     r8, r8
       jle      SHORT G_M000_IG09
       test     rsi, rsi
       jl       SHORT G_M000_IG09
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x30]
       cqo      
       idiv     rdx:rax, qword ptr [rbp-0x38]
       mov      qword ptr [rsi+0x08], rdx
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:
       mov      rsi, rax
       mov      edi, 6
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG10:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rax, gword ptr [rbp-0x108]
       test     rax, rax
       je       G_M000_IG35
 
G_M000_IG11:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG29
 
G_M000_IG12:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG35
 
G_M000_IG13:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG27
 
G_M000_IG14:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x58], rax
       lea      rcx, [rbp-0x78]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x70], rcx
       mov      r8, rsi
       sar      r8, 63
       and      r8, rax
       sub      rdx, r8
       mov      r8, rax
       sar      r8, 63
       and      r8, rsi
       sub      rdx, r8
       mov      r8, rcx
       sar      r8, 63
       cmp      rdx, r8
       jne      SHORT G_M000_IG17
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG15
       test     rax, rax
       jl       G_M000_IG65
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG64
       mov      rsi, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x70]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG16:
       jmp      G_M000_IG20
 
G_M000_IG17:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x118], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xFC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x118]
       test     rcx, rcx
       jne      SHORT G_M000_IG22
 
G_M000_IG18:
       test     rax, rax
       jne      SHORT G_M000_IG21
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0xFC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG19:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG20:
       jmp      G_M000_IG36
 
G_M000_IG21:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG23:
       test     rax, rax
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG25:
       mov      edx, dword ptr [rbp-0xFC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG19
 
G_M000_IG26:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG25
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG35
 
G_M000_IG28:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xA0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG36
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG35
 
G_M000_IG30:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG35
 
G_M000_IG31:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG33
 
G_M000_IG32:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG36
 
G_M000_IG33:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG36
 
G_M000_IG35:
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG36:
       test     rsi, rsi
       je       G_M000_IG61
 
G_M000_IG37:
       mov      rax, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG47
 
G_M000_IG38:
       mov      rax, qword ptr [rsi+0x08]
       test     r13, r13
       je       G_M000_IG61
 
G_M000_IG39:
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG45
 
G_M000_IG40:
       mov      r13, qword ptr [r13+0x08]
       lea      rcx, [rax+r13]
       mov      qword ptr [rbp-0xC8], rcx
       mov      rdx, rax
       xor      rdx, rcx
       mov      rsi, r13
       xor      rsi, rcx
       and      rdx, rsi
       jl       SHORT G_M000_IG43
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG41
       test     rax, rax
       jl       G_M000_IG65
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG64
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rdi, qword ptr [rbp-0xC8]
       mov      qword ptr [r13+0x08], rdi
 
G_M000_IG42:
       jmp      G_M000_IG44
 
G_M000_IG43:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      gword ptr [rbp-0x120], rax
       mov      ecx, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0x100], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       lea      rdi, [rbp-0xE8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x120]
       mov      esi, dword ptr [rbp-0x100]
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, qword ptr [rbp-0xE0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG44:
       jmp      G_M000_IG57
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG61
 
G_M000_IG46:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xF0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG57
 
G_M000_IG47:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG53
 
G_M000_IG48:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       test     r13, r13
       je       G_M000_IG61
 
G_M000_IG49:
       cmp      qword ptr [r13], rcx
       jne      SHORT G_M000_IG51
 
G_M000_IG50:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG57
 
G_M000_IG51:
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG61
 
G_M000_IG52:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG57
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG61
 
G_M000_IG54:
       mov      rdi, rsi
       test     r13, r13
       je       SHORT G_M000_IG61
 
G_M000_IG55:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      SHORT G_M000_IG61
 
G_M000_IG56:
       mov      rsi, r13
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
 
G_M000_IG57:
       add      r12, 1
       jo       G_M000_IG65
 
G_M000_IG58:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG62
 
G_M000_IG59:
       lea      rdi, [r12+0x05]
       cmp      rdi, 261
       ja       G_M000_IG03
 
G_M000_IG60:
       test     rdi, rdi
       jl       SHORT G_M000_IG65
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      SHORT G_M000_IG64
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      gword ptr [rbp-0x110], rax
       jmp      G_M000_IG04
 
G_M000_IG61:
       mov      rdx, r13
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
       jmp      SHORT G_M000_IG57
 
G_M000_IG62:
       mov      edi, 27
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG63:
       add      rsp, 248
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG64:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1758

