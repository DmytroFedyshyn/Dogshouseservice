using Dogshouseservice.Models;
using FluentValidation;

public class DogModelValidator : AbstractValidator<DogModel>
{
    public DogModelValidator()
    {
        RuleFor(d => d.Name)
            .NotEmpty().WithMessage("Dog name is required.");

        RuleFor(d => d.Color)
            .NotEmpty().WithMessage("Dog color is required.");

        RuleFor(d => d.TailLength)
            .GreaterThanOrEqualTo(0).WithMessage("Tail length must be non-negative.");

        RuleFor(d => d.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than zero.");
    }
}