using System;
using System.Collections.Generic;
using System.Text;

namespace WaveLabAgent.Resources
{
    public class ProcedureResult
    {
        public ProcedureResult(string path = "")
        {
            workspacePath = path;
        }
        public string workspacePath { get; set; }
        public Object Entity { get; set; }
    }
}
