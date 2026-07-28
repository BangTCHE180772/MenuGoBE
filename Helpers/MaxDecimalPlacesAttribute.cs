using System;
using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Helpers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MaxDecimalPlacesAttribute : ValidationAttribute
    {
        private readonly int _decimalPlaces;

        public MaxDecimalPlacesAttribute(int decimalPlaces)
        {
            _decimalPlaces = decimalPlaces;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null) return ValidationResult.Success;

            if (value is decimal decValue)
            {
                decimal factor = (decimal)Math.Pow(10, _decimalPlaces);
                if (decValue * factor != Math.Truncate(decValue * factor))
                {
                    return new ValidationResult(ErrorMessage ?? $"Số tiền chỉ được phép có tối đa {_decimalPlaces} chữ số thập phân.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
