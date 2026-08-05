using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Helpers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class IntegerOnlyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            if (value is decimal decValue)
            {
                if (decValue != Math.Truncate(decValue))
                {
                    return new ValidationResult(ErrorMessage ?? "Giá trị phải là số nguyên.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
