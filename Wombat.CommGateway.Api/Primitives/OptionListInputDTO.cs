using System.Collections.Generic;

namespace  Wombat.CommGateway.API
{
    public class OptionListInputDTO
    {
        public List<string> selectedValues { get; set; }
        public string q { get; set; }
    }
}