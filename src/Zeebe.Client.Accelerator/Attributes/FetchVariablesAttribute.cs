using System;
using Zeebe.Client.Accelerator.Abstractions;

namespace Zeebe.Client.Accelerator.Attributes 
{    
    public class FetchVariablesAttribute : AbstractJobHandlerAttribute
    {
        public FetchVariablesAttribute(params string[] fetchVariables)
        {
            if (fetchVariables is null || fetchVariables.Length == 0)
            {
                throw new ArgumentNullException(nameof(fetchVariables));
            }

            this.FetchVariables = fetchVariables;
        }

        public string[] FetchVariables { get; }
    }
}