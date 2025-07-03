

namespace Wombat.CommGateway.Infrastructure
{
    using System.ComponentModel.DataAnnotations;

    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _propertyName;
        private readonly object _expectedValue;

        public RequiredIfAttribute(string propertyName, object expectedValue)
        {
            _propertyName = propertyName;
            _expectedValue = expectedValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var propertyValue = type.GetProperty(_propertyName)?.GetValue(instance);

            if (propertyValue?.Equals(_expectedValue) == true && value == null)
            {
                return new ValidationResult(ErrorMessage ?? $"该字段在{_propertyName}为{_expectedValue}时必须填写");
            }
            return ValidationResult.Success;
        }
    }
}
