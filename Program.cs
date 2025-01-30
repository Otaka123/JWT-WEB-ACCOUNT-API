using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using Account_Web_Api.Data;
using Account_Web_Api.Models;
using Account_Web_Api.Models.Seed;
using Account_Web_Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("default"))
);
builder.Services.AddScoped<IJwtTokenServiceInterface, JwtTokenService>();

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Facebook";
})
.AddCookie()
.AddOAuth("Facebook", options =>
{
    options.ClientId = builder.Configuration["Authentication:Facebook:AppId"];
    options.ClientSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    options.CallbackPath = new PathString("/signin-facebook");
    options.AuthorizationEndpoint = "https://www.facebook.com/v12.0/dialog/oauth";
    options.TokenEndpoint = "https://graph.facebook.com/v12.0/oauth/access_token";
    options.UserInformationEndpoint = "https://graph.facebook.com/me?fields=id,name,email";

    options.SaveTokens = true;

    options.Events = new OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
            var response = await context.Backchannel.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error retrieving Facebook user info: {response.StatusCode}");
            }

            var user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:facebook:id", user.RootElement.GetString("id")));
            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:facebook:name", user.RootElement.GetString("name")));
            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:facebook:email", user.RootElement.GetString("email")));
        }
    };
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "http://localhost:7226",
        ValidAudience = "http://localhost:7226",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKey1234567890123456"))
    };
});
//}).AddFacebook(facebookOptions =>
// {
//     facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
//     facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
// })
//.AddGoogle(googleOptions =>
//{
//    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
//});

//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("DevRolePolicy", policy =>
//        policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Dev"));
//});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://localhost:7158") // استبدل بالنطاق المطلوب
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await Seed.Initialize(services, roleManager);  // تنفيذ Seed
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();


app.UseAuthorization();

app.MapControllers();

app.Run();
