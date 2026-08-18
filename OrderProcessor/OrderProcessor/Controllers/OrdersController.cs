using Microsoft.AspNetCore.Mvc;
using RefactoringExercise.Models;
using RefactoringExercise.Services;

namespace RefactoringExercise.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderProcessor orderProcessor) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResult>> ProcessOrder(
        ProcessRequest request, CancellationToken cancellationToken)
    {
        var result = await orderProcessor.ProcessOrderAsync(request, cancellationToken);

        return result.Outcome switch
        {
            OrderOutcome.Completed or OrderOutcome.CompletedEmailFailed => Ok(result),
            OrderOutcome.CustomerNotFound or OrderOutcome.ProductNotFound => NotFound(result),
            OrderOutcome.InvalidPaymentMethod => BadRequest(result),
            OrderOutcome.PaymentFailed => UnprocessableEntity(result),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result)
        };
    }

    [HttpGet("history/{customerId:int}")]
    public async Task<ActionResult<List<Order>>> GetHistory(
        int customerId, CancellationToken cancellationToken)
        => Ok(await orderProcessor.GetOrderHistoryAsync(customerId, cancellationToken));
}
