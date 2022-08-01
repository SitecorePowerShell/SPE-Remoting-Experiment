using System;
using System.Management.Automation;

namespace Spe.Commands
{
    public class BaseCommand : PSCmdlet
    {
        public void WriteToStream(PSObject item, bool shouldEnumerate)
        {
            if (item.TypeNames.Contains("Deserialized.System.Management.Automation.ErrorRecord"))
            {
                var errorRecord = new ErrorRecord(new Exception(item.Properties["Exception"].Value.ToString()), "", (ErrorCategory)Enum.Parse(typeof(ErrorCategory), item.Properties["ErrorCategory_Category"].Value.ToString()), null);
                WriteError(errorRecord);
            }
            else if (item.TypeNames.Contains("Deserialized.System.Management.Automation.WarningRecord"))
            {
                WriteWarning(item.ToString());
            }
            else if (item.TypeNames.Contains("Deserialized.System.Management.Automation.InformationRecord"))
            {
                var informationRecord = new InformationRecord(item.ToString(), "Sitecore PowerShell");
                WriteInformation(informationRecord);
            }
            else if (item.TypeNames.Contains("Deserialized.System.Management.Automation.DebugRecord"))
            {
                WriteDebug(item.ToString());
            }
            else if (item.TypeNames.Contains("Deserialized.System.Management.Automation.VerboseRecord"))
            {
                WriteVerbose(item.ToString());
            }
            else
            {
                WriteObject(item, shouldEnumerate);
            }
        }
    }
}
