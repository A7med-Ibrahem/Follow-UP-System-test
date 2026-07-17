using FluentValidation;
using SmartFollowUp.API.DTOs;

namespace SmartFollowUp.API.Validators
{
    public class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequestDto>
    {
        public CreatePrescriptionRequestValidator()
        {
            RuleFor(x => x.CaseId)
                .GreaterThan(0).WithMessage("Invalid Case ID");

            RuleFor(x => x.Instructions)
                .MaximumLength(1000).WithMessage("Instructions must not exceed 1000 characters");

            RuleFor(x => x.Medications)
                .NotEmpty().WithMessage("At least one medication is required");

            RuleForEach(x => x.Medications).SetValidator(new CreateMedicationDtoValidator());
        }
    }

    public class CreateMedicationDtoValidator : AbstractValidator<CreateMedicationDto>
    {
        public CreateMedicationDtoValidator()
        {
            RuleFor(x => x.MedicationName)
                .NotEmpty().WithMessage("Medication name is required")
                .MaximumLength(150).WithMessage("Medication name must not exceed 150 characters")
                .Matches(@"^[a-zA-Z\u0600-\u06FF0-9\s'\-\.,/]+$")
                    .WithMessage("Medication name contains invalid characters");

            RuleFor(x => x.Dosage)
                .NotEmpty().WithMessage("Dosage is required")
                .MaximumLength(100).WithMessage("Dosage must not exceed 100 characters");

            RuleFor(x => x.TimesPerDay)
                .InclusiveBetween(1, 6).WithMessage("Times per day must be between 1 and 6");

            RuleFor(x => x.DurationDays)
                .NotNull().WithMessage("Duration in days is required")
                .GreaterThan(0).WithMessage("Duration must be at least 1 day")
                .LessThanOrEqualTo(365).WithMessage("Duration must not exceed 365 days");
        }
    }
}
