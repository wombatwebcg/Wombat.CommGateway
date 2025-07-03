using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wombat.Extensions.DataTypeExtensions;

namespace Wombat.CommGateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ApiControllerBase
    {
        private readonly JwtOptions _jwtOptions;
        private IServiceProvider _serviceProvider;
        public PermissionsController(IServiceProvider serviceProvider,
            IOptions<JwtOptions> jwtOptions)
        {
            _serviceProvider = serviceProvider;
            _jwtOptions = jwtOptions.Value;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request.Username != "admin" || request.Password != "admin")
            {
                return Error("用户名或密码错误");
            }
            var devcieKey = _serviceProvider.GetService<IConfiguration>()?.GetSection($"Permissions:Devices").Get<string>();
            var claims = new[]
            {
                new Claim("Devcies",devcieKey)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                string.Empty,
                string.Empty,
                claims,
                expires: DateTime.Now.AddHours(_jwtOptions.AccessExpireHours),
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return Success(new
            {
                token = token,
                refreshToken = token,
                expiresIn = _jwtOptions.AccessExpireHours * 3600,
                user = new
                {
                    id = 1,
                    username = "admin",
                    name = "管理员",
                    roles = new[] { "admin" }
                }
            });
        }

        [HttpGet("user-info")]
        [AllowAnonymous]
        public IActionResult GetUserInfo()
        {
            // 从当前用户Claims中获取用户信息
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            // 这里模拟返回用户信息，实际应该从数据库获取
            return Success(new
            {
                id = 1,
                username = "admin",
                name = "管理员",
                role = "超级管理员",
                phone = "138****8888",
                email = "admin@example.com",
                avatar = "https://cube.elemecdn.com/0/88/03b0d39583f48206768a7534e55bcpng.png"
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // 验证原密码
            if (request.OldPassword != "admin")
            {
                return Error("原密码错误");
            }

            // 验证新密码
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                return Error("新密码不能为空");
            }

            if (request.NewPassword.Length < 6)
            {
                return Error("新密码长度不能小于6位");
            }

            // TODO: 实际应用中应该更新数据库中的密码
            // 这里仅作演示，实际应该对密码进行加密存储

            return Success("密码修改成功");
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return Error("刷新令牌不能为空");
            }

            var devcieKey = _serviceProvider.GetService<IConfiguration>()?.GetSection($"Permissions:Devices").Get<string>();
            if (devcieKey == null) return Error("系统配置错误");

            var claims = new[]
            {
                new Claim("Devcies", devcieKey)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                string.Empty,
                string.Empty,
                claims,
                expires: DateTime.Now.AddHours(_jwtOptions.AccessExpireHours),
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return Success(new
            {
                token = token,
                refreshToken = token,
                expiresIn = _jwtOptions.AccessExpireHours * 3600
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}


