using System;
using System.ComponentModel.DataAnnotations;
using Account_Web_Api.Validators;
namespace Account_Web_Api.DTO.RequestDTO
{
  

    public class RegisterDTO
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب.")]
        [StringLength(50, ErrorMessage = "الاسم الأول لا يمكن أن يتجاوز 50 حرفاً.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الأخير مطلوب.")]
        [StringLength(50, ErrorMessage = "الاسم الأخير لا يمكن أن يتجاوز 50 حرفاً.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب.")]
        [DataType(DataType.Date, ErrorMessage = "تنسيق التاريخ غير صالح.")]
        [CustomValidation(typeof(DateValidation), nameof(DateValidation.ValidateDOB))]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "وقت الإنشاء مطلوب.")]
        public DateTime Createtime { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
        [StringLength(30, MinimumLength = 5, ErrorMessage = "اسم المستخدم يجب أن يكون بين 5 و 30 حرفاً.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "تنسيق البريد الإلكتروني غير صالح.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 6 حروف.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string? Phone {  get; set; }
    }

}
