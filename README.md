# Spe

TODO:

- Add support for all default provider commands (such as Get-Item) with feature parity of the dynamic parameters.
- Add support for detecting available commands/functions found in the running instance and make available locally.
- Rehydrate objects with Type Converter

function Get-FunctionDefinition {
    param(
        [string]$Name
    )
    
    $MetaData = New-Object System.Management.Automation.CommandMetaData (Get-Command -Name $Name)
    $tokens = $errors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseInput([System.Management.Automation.ProxyCommand]::Create($MetaData), [ref]$tokens, [ref]$errors)
    
    # include all ast objects:
    #$predicate = { $true }
    
    $predicate = {
        param([System.Management.Automation.Language.Ast] $AST)
    
        $AST -is [System.Management.Automation.Language.ParamBlockAst]
    }
    
    # search for all ast objects, including nested scriptblocks:
    $recurse = $true
    
    # expose the object type:
    $type = @{
      Name = 'Type'
      Expression = { $_.GetType().Name }
    }
    
    # expose the code position:
    $startPosition = @{
      Name = 'StartPosition'
      Expression = { '{0}' -f $_.Extent.StartOffset }
    }
    $endPosition = @{
      Name = 'EndPosition'
      Expression = { '{0}' -f $_.Extent.EndOffset }
    }
    
    # expose the text of the code:
    $text = @{
      Name = 'Code'
      Expression = { $_.Extent.Text }
    }
    
    
    # find the ast objects:
    $astObjects = $ast.FindAll($predicate, $recurse)
    
    $paramBlock = $astObjects | Select-Object -Property $text | Select-Object -Expand Code
    
    $sb = New-Object System.Text.StringBuilder
    $sb.AppendLine("function $($Name) {") > $null
    $sb.AppendLine($paramBlock) > $null
    $sb.AppendLine("end { Invoke-RemoteScript -ScriptBlock { $($Name) @params } -ArgumentList `$PSBoundParameters }") > $null
    $sb.AppendLine("}") > $null
    
    $sb.ToString()
}

$excludeVerbs = @("Close", "Read", "Receive", "Send", "Show")
$excludeNouns = @("ScriptSession")
$commands = Get-Command | Where-Object { $_.ModuleName -eq "" -and $_.CommandType -eq "cmdlet" -and $excludeVerbs -notcontains $_.Verb -and $excludeNouns -notcontains $_.Noun } | Select-Object -Property Name
$commands | ForEach-Object { Get-FunctionDefinition -Name $_.Name }