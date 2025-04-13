using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DET.Common.Model
{
    public class ServiceDefinition
    {
        public string Interface { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
        public string Lifetime { get; set; } = "Scoped";
    }
}
