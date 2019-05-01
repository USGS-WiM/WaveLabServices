using System;
using System.Collections.Generic;
using System.Text;
using WIM.Utilities.Resources;

namespace WaveLabAgent.Resources
{
    public class ProcedureSettings
    {
        public Resource wavelab { get; set; }
        public List<Procedure> Procedures { get; set; }
        public List<ConfigurationOption> Configurations { get; set; }
        public Dictionary<string,List<int>> ProcedureConfigurations { get; set; }
    }
}
