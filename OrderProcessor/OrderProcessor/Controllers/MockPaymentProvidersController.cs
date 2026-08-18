using Microsoft.AspNetCore.Mvc;

namespace RefactoringExercise.Controllers;

[ApiController]
[Route("mock")]
public class MockPaymentProvidersController : ControllerBase
{
    [HttpGet("payments/charge")]
    public IActionResult ChargeCreditCard([FromQuery] decimal amount) => Charge(amount);

    [HttpGet("paypal/charge")]
    public IActionResult ChargePayPal([FromQuery] decimal amount) => Charge(amount);

    private ObjectResult Charge(decimal amount)
        => amount > 0
            ? StatusCode(StatusCodes.Status202Accepted, new { status = "success", amount })
            : BadRequest(new { status = "rejected", amount });
}
