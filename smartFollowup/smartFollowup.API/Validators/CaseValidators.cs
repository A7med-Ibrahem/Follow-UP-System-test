using FluentValidation;
using SmartFollowUp.API.DTOs;

namespace SmartFollowUp.API.Validators
{
    public class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequestDto>
    {
        public CreateCaseRequestValidator()
        {
            RuleFor(x => x.PatientName)
                .NotEmpty().WithMessage("Patient name is required")
                .MinimumLength(3).WithMessage("Patient name must be at least 3 characters")
                .MaximumLength(100).WithMessage("Patient name must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\u0600-\u06FF\s'\-\.]+$")
                    .WithMessage("Patient name must contain letters only (no numbers or symbols)");

            RuleFor(x => x.PatientEmail)
                .NotEmpty().WithMessage("Patient email is required")
                .EmailAddress().WithMessage("Please enter a valid email address.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters")
                .Must(email => email.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Email must be a @gmail.com address");

            RuleFor(x => x.PatientPhone)
                .NotEmpty().WithMessage("Patient phone is required")
                .Matches(@"^\+?[0-9]{8,15}$")
                    .WithMessage("Phone number must contain digits only (8 to 15 digits, optional leading +)");

            RuleFor(x => x.OperationType)
                .NotEmpty().WithMessage("Operation type is required")
                .MaximumLength(150).WithMessage("Operation type must not exceed 150 characters")
                .Matches(@"^[a-zA-Z\u0600-\u06FF\s'\-\.,/]+$")
                    .WithMessage("Operation type must contain letters only (no numbers)");

            RuleFor(x => x.OperationDate)
                .NotEmpty().WithMessage("Operation date is required")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("Operation date cannot be in the future");

            RuleFor(x => x.InitialTreatment)
                .MaximumLength(1000).WithMessage("Initial treatment must not exceed 1000 characters");
        }
    }
}