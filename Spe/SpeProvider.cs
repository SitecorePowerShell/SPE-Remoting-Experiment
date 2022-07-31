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

        private void ExecuteCommand(string script, string path)
        {
            var items = InvokeAndParse(script);

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

            object? current;
            while (queue.Count > 0 && (current = queue.Dequeue()) != null)
            {
                var currentPath = current.ToString();

                var items = InvokeAndParse($"Get-ChildItem -Path {GetItemPath(currentPath)}");

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
            var script = $"Remove-Item -Path {GetItemPath(path)} -Recurse:${recurse.ToString().ToLower()}";
            ExecuteCommand(script, path);
        }

        protected override void MoveItem(string path, string destination)
        {
            var script = $"Move-Item -Path {GetItemPath(path)} -Destination {destination}";
            ExecuteCommand(script, path);
        }

        protected override void RenameItem(string path, string newName)
        {
            var script = $"Rename-Item -Path {GetItemPath(path)} -NewName {newName}";
            ExecuteCommand(script, path);
        }

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            var parent = GetParentFromPath(path);
            var name = GetLeafFromPath(path);
            var script = $"New-Item -Path {GetItemPath(parent)} -Name {name} -ItemType '{itemTypeName}'";
            ExecuteCommand(script, path);
        }

        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            var script = $"Copy-Item -Path {GetItemPath(path)} -Destination {copyPath}";
            
            //If '-Recurse:$false' is provided then it will throw an error.
            if(recurse)
            {
                script += " -Recurse";
            }
            
            ExecuteCommand(script, path);
        }
    }
}
