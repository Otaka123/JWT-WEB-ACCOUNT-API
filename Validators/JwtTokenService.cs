
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Account_Web_Api.Models;
using Account_Web_Api.Validators;

public class JwtTokenService : IJwtTokenServiceInterface
{
    private readonly IConfiguration _configuration;
    //private readonly UserManager<User> _userManager;
    private readonly ILogger<JwtTokenService> _logger;
    private const string FacebookTokenValidationUrl = "https://graph.facebook.com/me?fields=id,name,email&access_token={0}";

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        //_userManager = userManager;
        _logger = logger;
    }
    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch
        {
            return null;
        }
    }
    public async Task<string> VerifyFacebookToken(string accessToken)
    {
        using (var client = new HttpClient())
        {
            var url = string.Format(FacebookTokenValidationUrl, accessToken);
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
    }
    public string GenerateJwtToken(User user, List<string> roles)
    {
        // التأكد من وجود المستخدم والأدوار
        if (user == null)
        {
            _logger.LogError("User not found.");
        }

        if (roles == null || !roles.Any())
        {
            _logger.LogError("Roles are required.");
        }

        // إضافة Claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),         // User ID
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique JWT ID
        };

        // إضافة الأدوار كـ Claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString().Trim())); // أفضل استخدام ClaimTypes.Role
        }

        // الحصول على المفتاح السري من الإعدادات (أفضل من تضمينه بشكل ثابت)
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogError("Secret key not found in configuration.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // تحديد وقت انتهاء التوكن من الإعدادات (قابل للتخصيص)
        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 30);
        var expirationTime = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],    // تأكد من أن الـ Issuer يتم تخزينه في الإعدادات
            audience: _configuration["Jwt:Audience"], // تأكد من أن الـ Audience يتم تخزينه في الإعدادات
            claims: claims,
            expires: expirationTime,
            signingCredentials: creds);

        _logger.LogInformation($"Generated JWT Token for user {user.UserName}");

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
