using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Security.Claims;
using Account_Web_Api.DTO.RequestDTO;
using Account_Web_Api.Models;
using Account_Web_Api.Validators;
using TodoAPI.Dtos.Auth.Request;

namespace Account_Web_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private IJwtTokenServiceInterface _authorizationService;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IJwtTokenServiceInterface authorizationService, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authorizationService = authorizationService;
            _signInManager = signInManager;
        }


        [HttpPost("register")]

        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("البيانات المدخلة غير صالحة");
            }

            User existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                return BadRequest("الايميل دا موجود قبل كدا");
            }

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Createtime = DateTime.UtcNow,
                DOB = model.DOB,
                PhoneNumber=model.Phone,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {

                var role = await _roleManager.FindByNameAsync("Dev");  // استخدام اسم الدور مباشرة
                if (role != null)
                {
                    await _userManager.AddToRoleAsync(user, role.Name);
                }
                return Ok(new { message = "تم تسجيل المستخدم بنجاح", userId = user.Id });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest($"فشل تسجيل المستخدم: {errors}");
        }



        [HttpPost("login")]
        public async Task<IActionResult> login([FromBody]LoginUserDTO model)
        {
            if (ModelState.IsValid)
            {
                //البحث عن المستخدم  
                User existingUser = await _userManager.Users
        .FirstOrDefaultAsync(u =>
            u.Email == model.Login || u.UserName == model.Login || u.PhoneNumber == model.Login);

                // إذا كان المستخدم غير موجود، نرجع رسالة خطأ
                if (existingUser == null)
                {
                    return BadRequest("Email address is not registered.");
                }

                // التحقق من صحة كلمة المرور
                bool isUserCorrect = await _userManager.CheckPasswordAsync(existingUser, model.Password);

                if (isUserCorrect)
                {
                    var roles = await _userManager.GetRolesAsync(existingUser);

                    var token = _authorizationService.GenerateJwtToken(existingUser, roles.ToList());
                    // إنشاء وتوليد الـ JWT Token عند نجاح التحقق
                    return Ok(new { Token = token });
                }
                else
                {
                    // إرجاع رسالة خطأ عند عدم تطابق كلمة المرور
                    return BadRequest("Invalid password.");
                }
            }
            else
            {
                // إرجاع تفاصيل الأخطاء عند فشل التحقق من النموذج
                return BadRequest(ModelState);
            }
        }
        //[HttpPost("facebook-login")]
        //public async Task<IActionResult> FacebookLogin([FromBody] string accessToken)
        //{
        //    // التحقق من الرمز المميز عبر Facebook
        //    var info = await _signInManager.GetExternalLoginInfoAsync();
        //    if (info == null)
        //    {
        //        return Unauthorized("Facebook token is invalid.");
        //    }

        //    // محاولة تسجيل الدخول عبر Facebook
        //    var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
        //    if (result.Succeeded)
        //    {
        //        return Ok("Login successful");
        //    }

        //    // إذا لم يكن المستخدم موجودًا، يمكنك إنشاء حساب جديد
        //    var user = new User { UserName = info.Principal.FindFirstValue(ClaimTypes.Email), Email = info.Principal.FindFirstValue(ClaimTypes.Email) };
        //    var createResult = await _userManager.CreateAsync(user);
        //    if (createResult.Succeeded)
        //    {
        //        await _userManager.AddLoginAsync(user, info);
        //        await _signInManager.SignInAsync(user, isPersistent: false);
        //        return Ok("User created and logged in.");
        //    }

        //    return BadRequest("Failed to create or log in user.");
        //}
        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] string accessToken)
        {
            var client = new HttpClient();

            // تحقق التوكن
            var verifyTokenUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token=<App_Access_Token>";
            var response = await client.GetStringAsync(verifyTokenUrl);
            var tokenInfo = JsonConvert.DeserializeObject<dynamic>(response);

            if (tokenInfo?.data?.is_valid != true)
            {
                return Unauthorized("Facebook token is invalid.");
            }

            // الحصول على معلومات المستخدم
            var userInfoUrl = $"https://graph.facebook.com/me?fields=id,email,name&access_token={accessToken}";
            var userInfoResponse = await client.GetStringAsync(userInfoUrl);
            var userInfo = JsonConvert.DeserializeObject<dynamic>(userInfoResponse);

            if (userInfo?.email == null)
            {
                return BadRequest("Email permission is required.");
            }

            // البحث عن المستخدم
            var user = await _userManager.FindByEmailAsync((string)userInfo.email);
            if (user == null)
            {
                user = new User { UserName = userInfo.email, Email = userInfo.email };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return BadRequest("Failed to create user.");
                }
            }

            // تسجيل الدخول
            await _signInManager.SignInAsync(user, isPersistent: false);
            return Ok("User logged in successfully.");
        }

        //[HttpGet("signin")]
        //public IActionResult SignIn()
        //{
        //    var redirectUrl = Url.Action("Callback");
        //    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        //    return Challenge(properties, "Facebook");
        //}

        //[HttpGet("callback")]
        //public async Task<IActionResult> Callback()
        //{
        //    var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        //    if (!authenticateResult.Succeeded)
        //        return Unauthorized();

        //    var claims = authenticateResult.Principal.Claims
        //        .ToDictionary(c => c.Type, c => c.Value);

        //    return Ok(new
        //    {
        //        AccessToken = authenticateResult.Properties.GetTokenValue("access_token"),
        //        User = claims
        //    });
        //}

        //[HttpPost("signout")]
        //public async Task<IActionResult> SignOutAsync()
        //{
        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //    return Ok(new { message = "Logged out successfully" });
        //}
        //[HttpPut("change-password")]
        //[Authorize]
        //public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        //البحث عن المستخدم  
        //        var user = await _userManager.GetUserAsync(User);

        //        //        User existingUser = await _userManager.Users
        //        //.FirstOrDefaultAsync(u =>u.Email == model.Email);

        //        // إذا كان المستخدم غير موجود، نرجع رسالة خطأ
        //        if (user == null)
        //        {
        //            return NotFound("User not found.");
        //        }

        //        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        //        if (!result.Succeeded)
        //        {
        //            return BadRequest(result.Errors);
        //        }


        //    }
        //    return Ok("Password changed successfully.");

        //}

        [HttpGet("info")]
        [Authorize(Roles = "Guest")]
        public IActionResult GetData()
        {
            return Ok("هذه البيانات متاحة للمبرمجين فقط.");
        }
        [HttpGet("infoP")]
        [Authorize(Roles = "Guest")]
        public IActionResult GetDataح()
        {
            return Ok("هذه البيانات متاحة للمبرمجين  والادمن فقط.");
        }


        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssginRoleDTO model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserID) || string.IsNullOrEmpty(model.RoleId))
            {
                return BadRequest("UserId and RoleId are required.");
            }

            try
            {

                var user = await _userManager.FindByIdAsync(model.UserID);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // 2. التحقق إذا كان الدور موجود
                var role = await _roleManager.FindByIdAsync(model.RoleId);
                if (role == null)
                {
                    return NotFound("Role does not exist.");
                }

                var result = await _userManager.AddToRoleAsync(user, role.Name);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return StatusCode(500,$"{errors}");
                }
                return Ok($"Role {model.RoleId} has been assigned to User {model.UserID}.");
            }
            catch (Exception ex)
            {
                return StatusCode(500,$"Error: {ex.Message}");
            }
        }

        [HttpPost("Add Role")]
        public async Task<IActionResult> AddRoleAsync(string roleName)
        {
            try
            {
                // إيجاد الدور باستخدام Role Id
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    return BadRequest("Role already exist.");
                }
                var Newrole = new IdentityRole(roleName);
                // حذف الدور
                var result = await _roleManager.CreateAsync(Newrole);
                if (result.Succeeded)
                {
                    return Ok($"Role {role.Name} has been Added successfully.");
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("Delete Role")]
        public async Task <IActionResult> DeleteRoleAsync(string roleId)
        {
            try
            {
                // إيجاد الدور باستخدام Role Id
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                {
                    return NotFound("Role does not exist.");
                }

                // حذف الدور
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return Ok($"Role {role.Name} has been deleted.");
                }
                else
                {
                    return BadRequest();
                }
            }
            catch(Exception ex) 
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpDelete("Delete Role From User")]
        public async Task<IActionResult> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            // إيجاد المستخدم باستخدام UserId
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User does not exist.");
            }

            // التحقق من وجود الدور
            var roleExist = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                return NotFound("Role does not exist.");
            }
            //اتاكد من ان الuser دا عنده الدور اصلا
            var Haverole = await _userManager.GetRolesAsync(user);
            if (Haverole == null)
            {
                return NotFound("User Dosn't have any Role.");

            }
            foreach (var role in Haverole)
            {
                if(role==roleName)
                {
                    var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                    if (result.Succeeded)
                    {
                        return Ok($"Role {roleName} has been removed from user {userId}.");
                    }
                    else
                    {
                        return StatusCode(500, $"Error: ");

                    }
                }
            }

            return BadRequest();
        }
        [HttpPut("Update roles")]
        public async Task<IActionResult> UpdateUserRolesAsync(string userId, [FromBody] List<string> roles)
        {
           
            if (roles == null || !roles.Any())
            {
                return BadRequest("Roles list cannot be empty.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User does not exist.");
            }

            // الحصول على الأدوار الحالية للمستخدم
            var userRoles = await _userManager.GetRolesAsync(user);

            // إيجاد الأدوار التي يجب إضافتها
            var rolesToAdd = roles.Except(userRoles).ToList();
            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    return StatusCode(500, "Failed to add new roles.");
                }
            }

            // إيجاد الأدوار التي يجب إزالتها
            var rolesToRemove = userRoles.Except(roles).ToList();
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    return StatusCode(500, "Failed to remove user roles.");
                }
            }

            return Ok("Roles updated successfully.");
        }

    }
}




