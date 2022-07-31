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

        protected override void GetItem(string path)
        {
            var items = InvokeAndParse($"Get-Item -Path {GetItemPath(path)}");

            foreach (var item in items)
            {
                WriteObject(item, path);
            }
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

        protected override void RemoveItem(string path, bool recurse)
        {
            var script = $"Remove-Item -Path {GetItemPath(path)} -Recurse:${recurse.ToString().ToLower()}";
            var items = InvokeAndParse(script);

            foreach (var item in items)
            {
                WriteObject(item, path);
            }
        }
    }
}
