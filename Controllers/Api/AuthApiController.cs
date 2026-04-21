using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartStock.Data;
using SmartStock.Models;
using SmartStock.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartStock.Controllers.Api
{
    [Route("api/auth")]
    [ApiController]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SmartStockDbContext _db;
        private readonly IEmailService _email;
        private readonly IConfiguration _config;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SmartStockDbContext db,
            IEmailService email,
            IConfiguration config)
        {
            _userManager = userManager;
            _db = db;
            _email = email;
            _config = config;
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ApiLoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Email and password are required." });

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null)
                return Unauthorized(new { error = "Invalid email or password." });

            if (await _userManager.IsInRoleAsync(user, "SystemAdmin"))
                return Unauthorized(new { error = "System administrators cannot use the mobile app." });

            if (user.TenantId.HasValue)
            {
                var tenant = await _db.Tenants.FindAsync(user.TenantId.Value);
                if (tenant != null && !tenant.IsActive)
                    return Unauthorized(new { error = "Your account has been suspended." });
            }

            if (await _userManager.IsLockedOutAsync(user))
                return Unauthorized(new { error = "Account locked. Try again in 15 minutes." });

            if (!await _userManager.CheckPasswordAsync(user, req.Password))
            {
                await _userManager.AccessFailedAsync(user);
                return Unauthorized(new { error = "Invalid email or password." });
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            if (!user.TwoFactorEnabled)
                await _userManager.SetTwoFactorEnabledAsync(user, true);

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            try { await _email.SendTwoFactorCodeAsync(user.Email!, user.FullName ?? user.Email!, code); }
            catch { /* continue even if email fails */ }

            return Ok(new { requiresTwoFactor = true, pendingToken = BuildPendingToken(user.Id) });
        }

        // POST /api/auth/verify
        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] ApiVerifyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.PendingToken) || string.IsNullOrWhiteSpace(req.Code))
                return BadRequest(new { error = "Token and code are required." });

            var userId = ValidatePendingToken(req.PendingToken);
            if (userId == null)
                return Unauthorized(new { error = "Session expired. Please login again." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { error = "User not found." });

            if (!await _userManager.VerifyTwoFactorTokenAsync(user, "Email", req.Code.Trim()))
                return Unauthorized(new { error = "Incorrect or expired code." });

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Cashier";

            return Ok(new
            {
                token = BuildAccessToken(user, role),
                role,
                tenantId = user.TenantId,
                userName = user.FullName ?? user.Email
            });
        }

        // ── helpers ──────────────────────────────────────────────────────

        private string BuildPendingToken(string userId)
        {
            var creds = GetCreds();
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("typ", "2fa-pending")
            };
            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"], _config["Jwt:Audience"],
                claims, expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string? ValidatePendingToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = GetKey(),
                    ValidateIssuer = true, ValidIssuer = _config["Jwt:Issuer"],
                    ValidateAudience = true, ValidAudience = _config["Jwt:Audience"],
                    ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(2)
                }, out _);

                return principal.FindFirstValue("typ") == "2fa-pending"
                    ? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    : null;
            }
            catch { return null; }
        }

        private string BuildAccessToken(ApplicationUser user, string role)
        {
            var creds = GetCreds();
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(ClaimTypes.Role, role),
                new Claim("tenantId", user.TenantId?.ToString() ?? ""),
                new Claim("fullName", user.FullName ?? user.Email!)
            };
            var hours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 8;
            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"], _config["Jwt:Audience"],
                claims, expires: DateTime.UtcNow.AddHours(hours),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private SymmetricSecurityKey GetKey() =>
            new(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        private SigningCredentials GetCreds() =>
            new(GetKey(), SecurityAlgorithms.HmacSha256);
    }

    public record ApiLoginRequest(string Email, string Password);
    public record ApiVerifyRequest(string PendingToken, string Code);
}
