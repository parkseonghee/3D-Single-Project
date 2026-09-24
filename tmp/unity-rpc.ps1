param([string]$Method='tools/call',[string]$Name='execute_code',[string]$Code,[string]$Uri)
$h=@{Accept='application/json, text/event-stream';'mcp-session-id'='4a66cf955b5849c8855fac0cf3eab13'}
$h['mcp-session-id']='4a66cf955b5849c885f5fac0cf3eab13'
$p=@{name=$Name;arguments=@{action='execute';code=$Code}}
if($Method -eq 'resources/read'){$p=@{uri=$Uri}}
if($Name -eq 'read_console'){$p=@{name=$Name;arguments=@{action='get';types=@('error');count='10';include_stacktrace=$true}}}
$b=@{jsonrpc='2.0';id=10;method=$Method;params=$p}|ConvertTo-Json -Depth 20
$r=Invoke-WebRequest http://127.0.0.1:8080/mcp -Method Post -ContentType application/json -Headers $h -Body $b
$r.Content
