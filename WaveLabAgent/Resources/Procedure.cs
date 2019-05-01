using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WIM.Resources;

namespace WaveLabAgent.Resources
{
    public class Procedure
    {
        public Int32 ID { get; set; }
        public string Code { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public List<ConfigurationOption> Configuration { get; set; }
        public List<Link> Links { get; set; }
    }
}
