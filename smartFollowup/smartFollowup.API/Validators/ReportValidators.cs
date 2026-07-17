using FluentValidation;
using SmartFollowUp.API.DTOs;

namespace SmartFollowUp.API.Validators
{
    public class CreateReportRequestValidator : AbstractValidator<CreateReportRequestDto>
    {
        public CreateReportRequestValidator()
        {
            RuleFor(x => x.CaseId)
                .GreaterThan(0).WithMessage("Invalid Case ID");

            RuleFor(x => x.Temperature)
                .InclusiveBetween(35, 42)
                .WithMessage("Temperature must be between 35 and 42°C");

            RuleFor(x => x.PainLevel)
                .InclusiveBetween(1, 10)
                .WithMessage("Pain level must be between 1 and 10");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
        }
    }

    public class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequestDto>
    {
        public UpdateReportRequestValidator()
        {
            RuleFor(x => x.Temperature)
                .InclusiveBetween(35, 42)
                .WithMessage("Temperature must be between 35 and 42°C");

            RuleFor(x => x.PainLevel)
                .InclusiveBetween(1, 10)
                .WithMessage("Pain level must be between 1 and 10");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters");
        }
    }
}