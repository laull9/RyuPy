; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     rbx
       push     rax
       lea      rbp, [rsp+0x20]
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 4
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      rbx, 2
       jl       SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       sub      rdi, 1
       jo       SHORT G_M000_IG07
       call     [RyuPy.Generated.Module:m0_fib_rec(long):long]
       mov      r14, rax
       mov      rdi, rbx
       sub      rdi, 2
       jo       SHORT G_M000_IG07
       call     [RyuPy.Generated.Module:m0_fib_rec(long):long]
       add      r14, rax
       jo       SHORT G_M000_IG07
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 5
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r14, rbx
 
G_M000_IG05:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG06:
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG07:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 155

; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data

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
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      r14d, r14d
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r13d, 1
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      r12d, r12d
       test     rbx, rbx
       jle      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       add      r14, r13
       jo       SHORT G_M000_IG08
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       add      r12, 1
       jo       SHORT G_M000_IG08
       cmp      r12, rbx
       jge      SHORT G_M000_IG06
 
G_M000_IG04:
       mov      rax, r13
       mov      r13, r14
       mov      r14, rax
       jmp      SHORT G_M000_IG03
 
G_M000_IG05:
       mov      r13, r14
 
G_M000_IG06:
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG07:
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG08:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 205

