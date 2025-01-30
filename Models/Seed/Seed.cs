using Microsoft.AspNetCore.Identity;

namespace Account_Web_Api.Models.Seed
{
    public class Seed
    {
       
            public static async Task Initialize(IServiceProvider serviceProvider, RoleManager<IdentityRole> roleManager)
            {
                var roleNames = new[] { "Admin", "Dev", "Manager", "Guest"  };

                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        var role = new IdentityRole(roleName);
                        await roleManager.CreateAsync(role);
                    }
                }
            }
        
    }
}
