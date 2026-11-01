; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      qword ptr [rbp-0x10], rax
       mov      qword ptr [rbp-0x08], rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x10], rax
       mov      edi, 4
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      qword ptr [rbp-0x08], 2
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x14], eax
       cmp      dword ptr [rbp-0x14], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 5
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x08]
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x08]
       sub      rax, 1
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x28], rax
       mov      rdi, qword ptr [rbp-0x28]
       call     [RyuPy.Generated.Module:m0_fib_rec(long):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rax, qword ptr [rbp-0x08]
       sub      rax, 2
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x38]
       call     [RyuPy.Generated.Module:m0_fib_rec(long):long]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x30]
       add      rax, qword ptr [rbp-0x40]
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x48], rax
       mov      rax, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG05:
       mov      rdi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, qword ptr [rbp-0x20]
 
G_M000_IG06:
       add      rsp, 80
       pop      rbp
       ret      
 
G_M000_IG07:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 238

; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 144
       lea      rbp, [rsp+0x90]
       xor      eax, eax
       mov      qword ptr [rbp-0x88], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      qword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x90], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      qword ptr [rbp-0x40], 1
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x60], rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x68], rax
       mov      qword ptr [rbp-0x70], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x90]
       dec      eax
       mov      dword ptr [rbp-0x90], eax
       cmp      dword ptr [rbp-0x90], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x90]
       mov      esi, 88
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x60]
       cmp      rax, qword ptr [rbp-0x68]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       cmp      dword ptr [rbp-0x74], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, qword ptr [rbp-0x40]
       jo       G_M000_IG09
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x40]
       mov      qword ptr [rbp-0x38], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x60]
       add      rax, qword ptr [rbp-0x70]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x60], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rbp-0x88], rax
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, qword ptr [rbp-0x88]
 
G_M000_IG08:
       add      rsp, 144
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 398

; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 144
       lea      rbp, [rsp+0x90]
       xor      eax, eax
       mov      qword ptr [rbp-0x88], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      qword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x90], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      qword ptr [rbp-0x40], 1
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x60], rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x68], rax
       mov      qword ptr [rbp-0x70], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x90]
       dec      eax
       mov      dword ptr [rbp-0x90], eax
       cmp      dword ptr [rbp-0x90], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x90]
       mov      esi, 88
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x60]
       cmp      rax, qword ptr [rbp-0x68]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       cmp      dword ptr [rbp-0x74], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, qword ptr [rbp-0x40]
       jo       G_M000_IG09
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x40]
       mov      qword ptr [rbp-0x38], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x60]
       add      rax, qword ptr [rbp-0x70]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x60], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rbp-0x88], rax
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, qword ptr [rbp-0x88]
 
G_M000_IG08:
       add      rsp, 144
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 398

; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 65112
; 8 inlinees with PGO data; 0 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     rbx
       push     rax
       lea      rbp, [rsp+0x10]
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, dword ptr [rdi+0x08]
       cmp      esi, r8d
       jae      G_M000_IG23
       cmp      dword ptr [rdi+4*rsi+0x10], 10
       je       SHORT G_M000_IG04
       mov      dword ptr [rdx+4*rsi+0x10], 10
 
G_M000_IG04:
       xor      edx, edx
       test     ecx, ecx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      r8, 0xD1FFAB1E
       mov      rsi, gword ptr [r8]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG23
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 11
       je       SHORT G_M000_IG06
       mov      dword ptr [rsi+4*r9+0x10], 11
 
G_M000_IG06:
       mov      edi, 1
       test     ecx, ecx
       jle      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      r8, rsi
       lea      r9d, [rcx-0x01]
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG23
       cmp      dword ptr [r8+4*r9+0x10], 12
       je       SHORT G_M000_IG08
       mov      dword ptr [rsi+4*r9+0x10], 12
 
G_M000_IG08:
       xor      esi, esi
       cmp      rsi, rbx
       jge      G_M000_IG17
       align    [0 bytes for IG09]
 
G_M000_IG09:
       test     ecx, ecx
       jle      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      r10, 0xD1FFAB1E
       mov      r9, gword ptr [r10]
       mov      r8, r9
       lea      r10d, [rcx-0x01]
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG23
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 13
       je       SHORT G_M000_IG11
       mov      dword ptr [r9+4*r11+0x10], 13
 
G_M000_IG11:
       add      rdx, rdi
       jo       G_M000_IG24
       test     ecx, ecx
       jle      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      r11, 0xD1FFAB1E
       mov      r9, gword ptr [r11]
       mov      r8, r9
       lea      r10d, [rcx-0x01]
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG23
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 14
       je       SHORT G_M000_IG13
       mov      dword ptr [r9+4*r11+0x10], 14
 
G_M000_IG13:
       test     ecx, ecx
       jle      SHORT G_M000_IG15
 
G_M000_IG14:
       mov      r11, 0xD1FFAB1E
       mov      r9, gword ptr [r11]
       mov      r8, r9
       lea      r10d, [rcx-0x01]
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG23
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 15
       je       SHORT G_M000_IG15
       mov      dword ptr [r9+4*r11+0x10], 15
 
G_M000_IG15:
       add      rsi, 1
       jo       SHORT G_M000_IG24
       cmp      rsi, rbx
       jge      SHORT G_M000_IG18
 
G_M000_IG16:
       mov      r8, rdx
       mov      rdx, rdi
       mov      rdi, r8
       jmp      G_M000_IG09
 
G_M000_IG17:
       mov      rdi, rdx
 
G_M000_IG18:
       test     ecx, ecx
       jle      SHORT G_M000_IG20
 
G_M000_IG19:
       mov      r11, 0xD1FFAB1E
       mov      r9, gword ptr [r11]
       mov      rdx, r9
       lea      r10d, [rcx-0x01]
       mov      esi, r10d
       mov      r11d, dword ptr [rdx+0x08]
       cmp      esi, r11d
       jae      SHORT G_M000_IG23
       mov      r11d, esi
       cmp      dword ptr [rdx+4*r11+0x10], 16
       je       SHORT G_M000_IG20
       mov      rdx, r9
       mov      dword ptr [rdx+4*r11+0x10], 16
 
G_M000_IG20:
       test     rax, rax
       jle      SHORT G_M000_IG21
       movsxd   rcx, ecx
       cmp      rax, rcx
       jg       SHORT G_M000_IG21
       dec      eax
       mov      dword ptr [(reloc)], eax
 
G_M000_IG21:
       mov      rax, rdi
 
G_M000_IG22:
       add      rsp, 8
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG23:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG24:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 533

; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      qword ptr [rbp-0x10], rax
       mov      qword ptr [rbp-0x08], rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x10], rax
       mov      edi, 4
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      qword ptr [rbp-0x08], 2
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x14], eax
       cmp      dword ptr [rbp-0x14], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 5
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x08]
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x08]
       sub      rax, 1
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x28], rax
       mov      rdi, qword ptr [rbp-0x28]
       call     [RyuPy.Generated.Module:m0_fib_rec(long):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rax, qword ptr [rbp-0x08]
       sub      rax, 2
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x38]
       call     [RyuPy.Generated.Module:m0_fib_rec(long):long]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x30]
       add      rax, qword ptr [rbp-0x40]
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x48], rax
       mov      rax, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, qword ptr [rbp-0x20]
 
G_M000_IG06:
       add      rsp, 80
       pop      rbp
       ret      
 
G_M000_IG07:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 268

