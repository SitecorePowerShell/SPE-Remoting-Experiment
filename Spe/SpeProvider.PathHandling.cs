using System;
using System.Linq;
using System.Management.Automation;

namespace Spe
{
    public partial class SpeProvider
    {
        protected override bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var items = InvokeAndParse($"Test-Path -Path {GetItemPath(path)}");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool)
                {
                    return (bool)items[0].ImmediateBaseObject;
                }
            }

            //log error
            return false;
        }

        protected override bool IsItemContainer(string path)
        {
            return true;
        }

        protected override bool ItemExists(string path)
        {
            //TODO: This seems a bit chatty.

            var items = InvokeAndParse($"Test-Path -Path {GetItemPath(path)}");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool)
                {
                    return (bool)items[0].ImmediateBaseObject;
                }
            }

            //log error
            return false;
        }

        protected override bool HasChildItems(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var items = InvokeAndParse($"Get-Item -Path {GetItemPath(path)} | Select-Object -Property HasChildren");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool)
                {
                    return (bool)items[0].ImmediateBaseObject;
                }
            }

            //log error
            return false;
        }

        private string GetItemPath(string path)
        {
            var colonIndex = path.IndexOf(':');
            var relativePath = path.Substring(colonIndex + 1).Replace('\\', '/');
            var databaseName = colonIndex < 0 ? PSDriveInfo.Name : path.Substring(0, colonIndex);

            return $"{databaseName}:{relativePath}";
        }

        private PSObject? GetItemForPath(string path)
        {
            return InvokeAndParse($"Get-Item -Path {GetItemPath(path)}").FirstOrDefault();
        }

        private string NormalizePath(string path)
        {
            var normalizedPath = path;
            if (string.IsNullOrEmpty(path)) return normalizedPath;

            normalizedPath = path.Replace('/', '\\');
            if (HasRelativePathTokens(path))
            {
                normalizedPath = NormalizeRelativePath(normalizedPath, null);
            }
            return normalizedPath;
        }

        private static bool HasRelativePathTokens(string path)
        {
            if ((path.IndexOf(@"\", StringComparison.OrdinalIgnoreCase) != 0) && !path.Contains(@"\.\") &&
                 !path.Contains(@"\..\") && !path.EndsWith(@"\..", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(@"\.", StringComparison.OrdinalIgnoreCase) &&
                  !path.StartsWith(@"..\", StringComparison.OrdinalIgnoreCase) &&
                 !path.StartsWith(@".\", StringComparison.OrdinalIgnoreCase))
            {
                return path.StartsWith("~", StringComparison.OrdinalIgnoreCase);
            }
            return true;
        }

        private static string GetParentFromPath(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            var lastLeafIndex = path.LastIndexOf('/');
            return path.Substring(0, lastLeafIndex);
        }

        private static string GetLeafFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            path = path.Replace('\\', '/').TrimEnd('/');
            var lastLeafIndex = path.LastIndexOf('/');
            return path.Substring(lastLeafIndex + 1);
        }
    }
}
