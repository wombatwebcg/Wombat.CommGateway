using System;

namespace  Wombat.CommGateway.Infrastructure
{
    public class MapAttribute : Attribute
    {
        public MapAttribute(params Type[] targetTypes)
        {
            TargetTypes = targetTypes;
        }
        public Type[] TargetTypes { get; }
    }
}
