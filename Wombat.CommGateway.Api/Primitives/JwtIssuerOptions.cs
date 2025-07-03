using System;
using System.Collections.Generic;
using System.Text;

namespace  Wombat.CommGateway.API
{
    /// <summary>
    /// 令牌
    /// </summary>
    public class JwtIssuerOptions
    {
        public string Issuer { get; set; }

        public string Audience { get; set; }

        public string SecretKey { get; set; }

        public int AccessExpiration { get; set; }

        public int RefreshExpiration { get; set; }

    }
}
