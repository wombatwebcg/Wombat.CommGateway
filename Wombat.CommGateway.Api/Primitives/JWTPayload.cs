using System;

namespace  Wombat.CommGateway.API
{
    public class JWTPayload
    {
        public string UserId { get; set; }
        public DateTime Expire { get; set; }
    }
}
