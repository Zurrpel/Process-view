Param(
    [switch]$All,          # показать цепочки для всех процессов
    [int]$ProcessId        # если указан, показать только для одного PID
)

# Кодировка консоли UTF-8 для корректного отображения кириллицы
if ($Host.UI.RawUI) {
    try {
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        [Console]::InputEncoding  = [System.Text.Encoding]::UTF8
    } catch { }
}
$OutputEncoding = [System.Text.Encoding]::UTF8

 $rawProcs = Get-CimInstance Win32_Process

 # Индекс процессов по PID из Get-Process (для CPU и affinity)
 $psProcsIndex = Get-Process |
     Group-Object -Property Id -AsHashTable -AsString

 # Расширенный список процессов с пользователем и информацией о CPU
 $procs = foreach ($p in $rawProcs) {
     $owner = $null
     try {
         $owner = Invoke-CimMethod -InputObject $p -MethodName GetOwner -ErrorAction Stop
     } catch {
         $owner = $null
     }

     $ownerName = if ($owner -and $owner.User) {
         if ($owner.Domain) {
             "$($owner.Domain)\$($owner.User)"
         } else {
             $owner.User
         }
     } else {
         "<SYSTEM/NO OWNER>"
     }

     $psProc = $null
     if ($psProcsIndex.ContainsKey($p.ProcessId.ToString())) {
         $psProc = $psProcsIndex[$p.ProcessId.ToString()]
     }

     $cpuSeconds = $null
     if ($psProc -and $psProc.CPU -ne $null) {
         $cpuSeconds = [double]$psProc.CPU
     }

     $affinityList = $null
     if ($psProc -and $psProc.ProcessorAffinity) {
         $mask = [int64]$psProc.ProcessorAffinity
         $bits = 0..63 | Where-Object { $mask -band (1L -shl $_) }
         if ($bits) {
             $affinityList = $bits -join ','
         }
     }

     [PSCustomObject]@{
         ProcessId       = $p.ProcessId
         Name            = $p.Name
         ParentProcessId = $p.ParentProcessId
         User            = $ownerName
         CpuTotalSeconds = $cpuSeconds
         CpuAffinity     = $affinityList
     }
 }

$processIndex = $procs |
    Group-Object -Property ProcessId -AsHashTable -AsString

# Индекс ParentPid -> список дочерних процессов
$childrenIndex = @{}
foreach ($p in $procs) {
    $ppidKey = $p.ParentProcessId.ToString()
    if (-not $childrenIndex.ContainsKey($ppidKey)) {
        $childrenIndex[$ppidKey] = @()
    }
    $childrenIndex[$ppidKey] += $p
}

function Get-ParentChain {
    param(
        [int]$StartPid
    )

    $chain = @()
    $currentPid = $StartPid
    $visited = New-Object System.Collections.Generic.HashSet[int]

    while ($currentPid -and $currentPid -ne 0 -and -not $visited.Contains($currentPid)) {
        $visited.Add($currentPid) | Out-Null

        $proc = $processIndex[$currentPid.ToString()]
        if (-not $proc) {
            $chain += [PSCustomObject]@{
                ProcessId = $currentPid
                Name      = '<unknown, no data>'
                User      = '<unknown>'
                CpuTotalSeconds = $null
                CpuAffinity     = $null
            }
            break
        }

        $chain += [PSCustomObject]@{
            ProcessId = $proc.ProcessId
            Name      = $proc.Name
            User      = $proc.User
            CpuTotalSeconds = $proc.CpuTotalSeconds
            CpuAffinity     = $proc.CpuAffinity
        }

        $currentPid = $proc.ParentProcessId
    }

    return $chain
}

function Show-ProcessTree {
    param(
        [int]$RootPid,
        [string]$Prefix = "",
        [bool]$IsLast = $true,
        [System.Collections.Generic.HashSet[int]]$Visited
    )

    if ($Visited.Contains($RootPid)) {
        Write-Host ("{0}└─ [cycle on PID {1}]" -f $Prefix, $RootPid)
        return
    }
    $Visited.Add($RootPid) | Out-Null

    $proc = $processIndex[$RootPid.ToString()]
    if (-not $proc) {
        Write-Host ("{0}└─ <unknown>({1})" -f $Prefix, $RootPid)
        return
    }

    # Ветка: корень рисуем без псевдографики, его детей уже с "├─/└─"
    if ($Prefix -eq "") {
        $branch = ""
    } else {
        $branch = if ($IsLast) { "└─ " } else { "├─ " }
    }
    $cpuText = if ($proc.CpuTotalSeconds -ne $null) {
        "{0:N1}s" -f $proc.CpuTotalSeconds
    } else {
        "n/a"
    }

    $affinityText = if ($proc.CpuAffinity) {
        $proc.CpuAffinity
    } else {
        "all"
    }

    $userText = if ($proc.User) { $proc.User } else { "<unknown>" }

    $line = "{0}{1}{2}({3}) [PPID:{4}] [User:{5}] [CPU:{6}] [Affinity:{7}]" -f `
        $Prefix, $branch, $proc.Name, $proc.ProcessId, $proc.ParentProcessId, $userText, $cpuText, $affinityText
    Write-Host $line

    $children = @()
    $rootKey = $RootPid.ToString()
    if ($childrenIndex.ContainsKey($rootKey)) {
        $children = $childrenIndex[$rootKey] | Sort-Object Name, ProcessId
    }

    for ($i = 0; $i -lt $children.Count; $i++) {
        $child = $children[$i]
        $lastChild = ($i -eq $children.Count - 1)

        # Классическая отрисовка дерева с вертикалями на всех уровнях.
        if ($Prefix -eq "") {
            # Первый уровень под корнем: вертикаль, если после узла ещё есть братья.
            $newPrefix = if ($lastChild) { "   " } else { "│  " }
        } else {
            # Начиная со второго уровня добавляем либо пробелы, либо вертикаль
            # в зависимости от того, является ли текущий узел последним в своём списке.
            $newPrefix = if ($IsLast) {
                $Prefix + "   "
            } else {
                $Prefix + "│  "
            }
        }

        Show-ProcessTree -RootPid $child.ProcessId -Prefix $newPrefix -IsLast $lastChild -Visited $Visited
    }
}

if ($All) {
    Write-Host "Process tree for all processes (forest of roots):`n"

    # Корневые процессы: у которых нет родителя в нашей выборке или ParentProcessId = 0
    $rootProcs = $procs |
        Where-Object {
            ($_.ParentProcessId -eq 0) -or
            (-not $processIndex.ContainsKey($_.ParentProcessId.ToString()))
        } |
        Sort-Object Name, ProcessId

    $visitedGlobal = [System.Collections.Generic.HashSet[int]]::new()

    foreach ($root in $rootProcs) {
        if ($visitedGlobal.Contains($root.ProcessId)) { continue }

        $visited = [System.Collections.Generic.HashSet[int]]::new()
        Show-ProcessTree -RootPid $root.ProcessId -Visited $visited

        foreach ($visitedPid in $visited) {
            $visitedGlobal.Add($visitedPid) | Out-Null
        }

        Write-Host
    }
} else {
    if (-not $ProcessId) {
        Write-Error 'You must specify -ProcessId or use -All.'
        exit 1
    }

    if (-not $processIndex.ContainsKey($ProcessId.ToString())) {
        Write-Error "Process with PID $ProcessId not found."
        exit 1
    }
    Write-Host ("Parent chain for PID {0}`n" -f $ProcessId)

    $chain = Get-ParentChain -StartPid $ProcessId
    foreach ($item in $chain) {
        $cpuText = if ($item.CpuTotalSeconds -ne $null) {
            "{0:N1}s" -f $item.CpuTotalSeconds
        } else {
            "n/a"
        }

        $affinityText = if ($item.CpuAffinity) {
            $item.CpuAffinity
        } else {
            "all"
        }

        "{0,8}  {1,-30}  {2,-25}  CPU:{3,-8}  Affinity:{4}" -f `
            $item.ProcessId, $item.Name, $item.User, $cpuText, $affinityText
    }
}


