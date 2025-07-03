using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using  Wombat.CommGateway.Infrastructure;

namespace  Wombat.CommGateway.Infrastructure
{
    public static partial class Extention
    {
        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
        {
            // 明确先注册 JwtOptions
            var jwtSection = configuration.GetSection("jwt");
            services.Configure<JwtOptions>(jwtSection);

            // 提前绑定是没问题的，但不要依赖它注入
            var jwtOptions = jwtSection.Get<JwtOptions>();

            if (jwtOptions != null)
            {
                services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = false;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ClockSkew = TimeSpan.Zero,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                    };
                });
            }

            return services;
        }

        //public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
        //{

        //    var section = configuration.GetSection("jwt");
        //    services.Configure<JwtOptions>(section);
        //    var jwtOptions = configuration.GetSection("jwt").Get<JwtOptions>();
        //    if (jwtOptions != null) {
        //    services.AddAuthentication(x =>
        //    {
        //        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //    }).AddJwtBearer(x =>
        //    {
        //        x.RequireHttpsMetadata = false;
        //        x.SaveToken = false;
        //        //Token Validation Parameters
        //        x.TokenValidationParameters = new TokenValidationParameters
        //        {
        //            ClockSkew = TimeSpan.Zero,//到时间立即过期
        //            ValidateIssuerSigningKey = true,
        //            //获取或设置要使用的Microsoft.IdentityModel.Tokens.SecurityKey用于签名验证。
        //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.
        //            GetBytes(jwtOptions.Secret)),
        //            ValidateIssuer = false,
        //            ValidateAudience = false,
        //        };
        //    });
        //    }
        //    return services;
        //}
    }
}
