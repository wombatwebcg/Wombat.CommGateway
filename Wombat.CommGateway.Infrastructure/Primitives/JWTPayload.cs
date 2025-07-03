using System;

namespace  Wombat.CommGateway.Infrastructure
{
    public class JWTPayload
    {
        public string UserId { get; set; }
        public DateTime Expire { get; set; }
    }
}
