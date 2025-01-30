using Google.Apis.Auth;
using Account_Web_Api.Models;

namespace Account_Web_Api.Validators
{
    public interface IJwtTokenServiceInterface
    {
        string GenerateJwtToken(User user, List<string> roles);
        Task<string> VerifyFacebookToken(string accessToken);
        Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken);
    }
}
