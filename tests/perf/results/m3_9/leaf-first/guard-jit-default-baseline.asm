; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 208
       lea      rbp, [rsp+0xD0]
       xor      eax, eax
       mov      qword ptr [rbp-0xC8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xD0], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xD0]
       dec      eax
       mov      dword ptr [rbp-0xD0], eax
       cmp      dword ptr [rbp-0xD0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xD0]
       mov      esi, 86
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x88], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x8C]
       and      eax, dword ptr [rbp-0x90]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       cmp      dword ptr [rbp-0x8C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0x94]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x98]
       or       eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       cmp      dword ptr [rbp-0xA4], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rdx, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0xB8]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:CallValue1(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG08:
       add      rsp, 208
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 594

; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x56
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 11 single block inlinees; 9 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x148], r15
       mov      qword ptr [rsp+0x140], r14
       mov      qword ptr [rsp+0x138], r13
       mov      qword ptr [rsp+0x130], r12
       mov      qword ptr [rsp+0x128], rbx
       lea      rbp, [rsp+0x70]
       mov      rbx, gword ptr [rbp+0xA8]
       mov      rax, gword ptr [rbp+0xA0]
       mov      r12, qword ptr [rbp+0x90]
       mov      r15, qword ptr [rbp+0x68]
       mov      r14, qword ptr [rbp+0x60]
       mov      r13, qword ptr [rbp+0x58]
 
G_M000_IG02:
       test     r13, r13
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0x30], ecx
       jmp      G_M000_IG18
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG04:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp+0x28], rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x60], rcx
       mov      rdx, gword ptr [rcx+0x18]
       test     rdx, rdx
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       cmp      dword ptr [rdx+0x08], 1
       mov      gword ptr [rbp-0x48], rdx
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x60]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG07:
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, rax
       mov      rdx, rbx
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x28]
       test     rax, rax
       je       SHORT G_M000_IG11
 
G_M000_IG08:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG26
 
G_M000_IG09:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG25
 
G_M000_IG10:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG24
 
G_M000_IG11:
       xor      r9d, r9d
 
G_M000_IG12:
       mov      dword ptr [rbp-0x2C], r9d
       mov      rcx, 0xD1FFAB1E
       mov      r10, gword ptr [rcx]
       mov      gword ptr [rbp-0x68], r10
       lea      rcx, bword ptr [r10+0x50]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG27
 
G_M000_IG13:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG27
 
G_M000_IG14:
       cmp      r9d, 1
       jne      G_M000_IG42
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG16
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG41
 
G_M000_IG16:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG17:
       add      r15, r13
       jo       G_M000_IG46
       mov      ecx, dword ptr [rbp-0x30]
 
G_M000_IG18:
       mov      edi, ecx
       cmp      r15, r14
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       cmp      r15, r14
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG29
 
G_M000_IG19:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG22
 
G_M000_IG20:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rdx, rax
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG45
       cmp      dword ptr [rdx+4*rsi+0x10], 13
       je       SHORT G_M000_IG22
 
G_M000_IG21:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG45
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 13
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x38], rax
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG04
       jmp      G_M000_IG03
 
G_M000_IG24:
       mov      r9d, 3
       jmp      G_M000_IG12
 
G_M000_IG25:
       mov      r9d, 2
       jmp      G_M000_IG12
 
G_M000_IG26:
       mov      r9d, 1
       jmp      G_M000_IG12
 
G_M000_IG27:
       lea      rcx, bword ptr [r10+0x60]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG36
 
G_M000_IG28:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG36
       jmp      G_M000_IG14
 
G_M000_IG29:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG45
       cmp      dword ptr [rdx+4*r8+0x10], 14
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG45
       mov      dword ptr [rcx+4*rdx+0x10], 14
 
G_M000_IG32:
       test     r12, r12
       jle      SHORT G_M000_IG35
 
G_M000_IG33:
       movsxd   rcx, edi
       cmp      r12, rcx
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       dec      r12d
       mov      dword ptr [(reloc)], r12d
 
G_M000_IG35:
       add      rsp, 296
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG36:
       lea      rcx, bword ptr [r10+0x70]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG38
 
G_M000_IG37:
       cmp      gword ptr [rcx], rax
       je       G_M000_IG14
 
G_M000_IG38:
       lea      rcx, bword ptr [r10+0x80]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG40
       mov      r10, gword ptr [rbp-0x68]
       jmp      G_M000_IG14
 
G_M000_IG40:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x50], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       G_M000_IG44
       jmp      SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rsi, rax
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG42:
       cmp      r9d, 3
       jne      SHORT G_M000_IG40
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG17
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 17
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 4
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x68]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x58], rdx
       mov      rsi, gword ptr [rbp+0x28]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x58]
       mov      byte  ptr [rsi+0x08], 1
       mov      edi, dword ptr [rbp-0x2C]
       mov      byte  ptr [rsi+0x09], dil
 
G_M000_IG44:
       mov      rax, gword ptr [rbp-0x50]
       jmp      G_M000_IG17
 
G_M000_IG45:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG46:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1035

