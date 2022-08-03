using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spe.Commands
{
    [Cmdlet(VerbsCommon.Get, "FunctionDefinition")]
    public class GetFunctionDefinitionCommand : BaseCommand
    {
        [Parameter]
        public string Name { get; set; }

        protected override void EndProcessing()
        {
            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine(Scripts.Get_FunctionDefinition);
            scriptBuilder.AppendLine("$excludeVerbs = @('Close', 'Read', 'Receive', 'Send', 'Show')");
            scriptBuilder.AppendLine("$excludeNouns = @('ScriptSession')");

            if (!string.IsNullOrEmpty(Name))
            {
                scriptBuilder.AppendLine("$commands = Get-Command -Name '" + Name + "' | Where-Object { $_.ModuleName -eq '' -and $_.CommandType -eq 'cmdlet' -and $excludeVerbs -notcontains $_.Verb -and $excludeNouns -notcontains $_.Noun } | Select-Object -Property Name");
            }
            else
            {
                scriptBuilder.AppendLine("$commands = Get-Command | Where-Object { $_.ModuleName -eq '' -and $_.CommandType -eq 'cmdlet' -and $excludeVerbs -notcontains $_.Verb -and $excludeNouns -notcontains $_.Noun } | Select-Object -Property Name");
            }
            
            scriptBuilder.AppendLine("$commands | ForEach-Object { Get-FunctionDefinition -Name $_.Name }");

            var items = RemotingHelper.InvokeAndParse(scriptBuilder.ToString());
            
            foreach (var item in items)
            {
                var functionText = Regex.Replace(item.ToString(), @"\[[a-zA-Z]{1,50}[.][a-zA-Z.]{1,100}(\[\])?\]", "[PSObject]");

                WriteToStream(functionText, true);
                var results = base.InvokeCommand.InvokeScript(item.ToString());
                foreach(var result in results)
                {
                    WriteToStream(result, true);
                }                
            }
        }
    }
}
