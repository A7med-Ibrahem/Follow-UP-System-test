using FluentValidation;
using SmartFollowUp.API.DTOs;

namespace SmartFollowUp.API.Validators
{
    public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequestDto>
    {
        public CreateNoteRequestValidator()
        {
            RuleFor(x => x.CaseId)
                .GreaterThan(0).WithMessage("Invalid Case ID");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Note content is required")
                .MinimumLength(2).WithMessage("Note content is too short")
                .MaximumLength(2000).WithMessage("Note content must not exceed 2000 characters");
        }
    }
}
