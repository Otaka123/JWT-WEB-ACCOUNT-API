using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TodoAPI.Dtos.Auth.Request;

public class LoginUserDTO
{
    [Required]
    public string Login { get; set; }

    [Required]
    public string Password { get; set; }
}
