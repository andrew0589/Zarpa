# Crea la tarea "NavigationES Instagram": cada 15 minutos publica en Instagram lo que toca.
# Para quitarla: Unregister-ScheduledTask -TaskName "NavigationES Instagram" -Confirm:$false
$aici = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonw = Join-Path (Split-Path -Parent (Get-Command python).Source) "pythonw.exe"

$a = New-ScheduledTaskAction -Execute $pythonw -Argument "`"$aici\programa_instagram.py`" publica-scadente" -WorkingDirectory $aici
$t = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(2) -RepetitionInterval (New-TimeSpan -Minutes 15)
$s = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 10) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName "NavigationES Instagram" -Action $a -Trigger $t -Settings $s -Force `
    -Description "Publica en Instagram la Pregunta del dia (Documentation\SocialMedia\programa_instagram.py)" | Out-Null

Get-ScheduledTask -TaskName "NavigationES Instagram" | Get-ScheduledTaskInfo | Select-Object TaskName, NextRunTime
