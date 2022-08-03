using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Spe
{
    [CmdletProvider("Spe", ProviderCapabilities.ShouldProcess)]
    public partial class SpeProvider : NavigationCmdletProvider
    {
        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            return drive;
        }

        private void WriteObject(PSObject item, string path)
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
                WriteItemObject(item, path, true);
            }
        }

        private void ExecuteCommand(string script, string path)
        {
            var items = RemotingHelper.InvokeAndParse(script);

            foreach (var item in items)
            {
                WriteObject(item, path);
            }
        }

        protected override void GetItem(string path)
        {
            var script = $"Get-Item -Path {GetItemPath(path)}";
            ExecuteCommand(script, path);
        }

        protected override void GetChildItems(string path, bool recurse)
        {
            var queue = new Queue();
            queue.Enqueue(path);

            object current;
            while (queue.Count > 0 && (current = queue.Dequeue()) != null)
            {
                var currentPath = current.ToString();

                //Let's call without recurse to reduce the volume of data per request.
                var items = RemotingHelper.InvokeAndParse($"Get-ChildItem -Path {GetItemPath(currentPath)}");

                foreach (PSObject item in items)
                {
                    WriteObject(item, path);

                    if (!recurse) continue;

                    if (!bool.TryParse(item.Properties["HasChildren"].Value.ToString(), out bool hasChildren) || !hasChildren) continue;

                    var itemPath = item.Properties["ItemPath"].Value.ToString();
                    queue.Enqueue(itemPath);
                }
            }
        }

        protected override string GetChildName(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var result = GetLeafFromPath(path);

            return result;
        }

        protected override void RemoveItem(string path, bool recurse)
        {
            if (!ShouldProcess(path)) return;

            var script = $"Remove-Item -Path {GetItemPath(path)}";
            if (recurse)
            {
                script += " -Recurse";
            }

            ExecuteCommand(script, path);
        }

        protected override void MoveItem(string path, string destination)
        {
            if (!ShouldProcess(path)) return;

            var script = $"Move-Item -Path {GetItemPath(path)} -Destination {destination}";
            ExecuteCommand(script, path);
        }

        protected override void RenameItem(string path, string newName)
        {
            if (!ShouldProcess(path)) return;

            var script = $"Rename-Item -Path {GetItemPath(path)} -NewName {newName}";
            ExecuteCommand(script, path);
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            if (!ShouldProcess(path)) return;

            var parent = GetParentFromPath(path);
            var name = GetLeafFromPath(path);
            var script = $"New-Item -Path {GetItemPath(parent)} -Name {name} -ItemType '{itemTypeName}'";
            ExecuteCommand(script, path);
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            if (!ShouldProcess(path)) return;

            var script = $"Copy-Item -Path {GetItemPath(path)} -Destination {copyPath}";

            //If '-Recurse:$false' is provided then it will throw an error.
            if (recurse)
            {
                script += " -Recurse";
            }

            ExecuteCommand(script, path);
        }
    }
}
