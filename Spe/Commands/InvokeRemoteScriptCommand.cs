using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Spe.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "RemoteScript")]
    public class InvokeRemoteScriptCommand : BaseCommand
    {
        [Parameter(Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; }

        [Parameter]
        [Alias("Arguments")]
        public object ArgumentList { get; set; }

        protected override void EndProcessing()
        {
            PSObject? arguments = null;
            if(ArgumentList != null)
            {
                arguments = new PSObject(ArgumentList);
            }
            var items = RemotingHelper.InvokeAndParse(ScriptBlock.ToString(), arguments);

            foreach (var item in items)
            {
                WriteToStream(item, true);
            }
        }
    }
}
