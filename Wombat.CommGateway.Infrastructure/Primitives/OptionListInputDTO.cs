using System.Collections.Generic;

namespace  Wombat.CommGateway.Infrastructure
{
    public class OptionListInputDTO
    {
        public List<string> selectedValues { get; set; }
        public string q { get; set; }
    }
}