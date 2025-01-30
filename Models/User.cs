using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Account_Web_Api.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date, ErrorMessage = "تنسيق التاريخ غير صالح.")]
        public DateTime DOB { get; set; }
        public DateTime Createtime {  get; set; }
     }
}
