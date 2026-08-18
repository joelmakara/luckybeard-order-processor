using System.ComponentModel.DataAnnotations;
using RefactoringExercise.Models;

namespace RefactoringExercise.Tests;

public class ProcessRequestValidationTests
{
    private static List<ValidationResult> Validate(ProcessRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static ProcessRequest ValidRequest() => new()
    {
        CustomerId = 1,
        CustomerEmail = "ada@example.com",
        Items = ["Keyboard"],
        PaymentMethod = "CreditCard",
        DiscountPercentage = 10
    };

    [Fact]
    public void Valid_request_passes()
        => Assert.Empty(Validate(ValidRequest()));

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails(string email)
    {
        var request = ValidRequest();
        request.CustomerEmail = email;
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void Empty_items_fail()
    {
        var request = ValidRequest();
        request.Items = [];
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void Whitespace_item_fails()
    {
        var request = ValidRequest();
        request.Items = [" "];
        Assert.NotEmpty(Validate(request));
    }

    [Theory]
    [InlineData("Cheque")]
    [InlineData("2")]
    [InlineData("")]
    public void Unknown_payment_method_fails(string method)
    {
        var request = ValidRequest();
        request.PaymentMethod = method;
        Assert.NotEmpty(Validate(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Discount_out_of_range_fails(int discountPercentage)
    {
        var request = ValidRequest();
        request.DiscountPercentage = discountPercentage;
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void Non_positive_customer_id_fails()
    {
        var request = ValidRequest();
        request.CustomerId = 0;
        Assert.NotEmpty(Validate(request));
    }
}
