using System.ComponentModel.DataAnnotations;
using Account_Web_Api.DTO.RequestDTO;

namespace Account_Web_Api.Validators
{
    public static class DateValidation
    {
        public static ValidationResult ValidateDOB(DateTime dob)
        {
            if (dob > DateTime.UtcNow)
            {
                return new ValidationResult("تاريخ الميلاد لا يمكن أن يكون في المستقبل.");
            }

            if ((DateTime.UtcNow.Year - dob.Year) < 18)
            {
                return new ValidationResult("يجب أن يكون عمر المستخدم 18 سنة أو أكثر.");
            }

            return ValidationResult.Success;
        }
       
    }

}
