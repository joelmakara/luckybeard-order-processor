using System.ComponentModel.DataAnnotations;

namespace RefactoringExercise.Models;

public class ProcessRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [MinLength(1)]
    public List<string> Items { get; set; } = [];

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items.Any(string.IsNullOrWhiteSpace))
        {
            yield return new ValidationResult(
                "Items must not contain empty entries.", [nameof(Items)]);
        }

        if (int.TryParse(PaymentMethod, out _) || !Enum.TryParse<PaymentMethod>(PaymentMethod, out _))
        {
            yield return new ValidationResult(
                $"Payment method must be one of: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.",
                [nameof(PaymentMethod)]);
        }
    }
}
