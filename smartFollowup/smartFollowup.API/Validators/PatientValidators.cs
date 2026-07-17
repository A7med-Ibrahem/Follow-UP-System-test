using FluentValidation;
using SmartFollowUp.API.DTOs;

namespace SmartFollowUp.API.Validators
{
    public class UpdatePatientProfileRequestValidator : AbstractValidator<UpdatePatientProfileRequestDto>
    {
        public UpdatePatientProfileRequestValidator()
        {
            RuleFor(x => x.Phone)
                .Matches(@"^\+?[0-9]{8,15}$")
                    .WithMessage("Phone number must contain digits only (8 to 15 digits, optional leading +)")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.Age)
                .InclusiveBetween(0, 120).WithMessage("Age must be between 0 and 120")
                .When(x => x.Age.HasValue);

            RuleFor(x => x.Gender)
                .Must(g => string.Equals(g, "Male", StringComparison.OrdinalIgnoreCase) || string.Equals(g, "Female", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Gender must be either Male or Female")
                .When(x => !string.IsNullOrWhiteSpace(x.Gender));

            RuleFor(x => x.ChronicDiseases)
                .MaximumLength(500).WithMessage("Chronic diseases must not exceed 500 characters");

            RuleFor(x => x.Allergies)
                .MaximumLength(500).WithMessage("Allergies must not exceed 500 characters");

            RuleFor(x => x.CurrentMedications)
                .MaximumLength(500).WithMessage("Current medications must not exceed 500 characters");
        }
    }
}
