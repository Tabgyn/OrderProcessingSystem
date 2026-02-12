using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Queries;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IPlaceOrderCommandHandler _placeOrderHandler;
    private readonly IGetOrderQueryHandler _getOrderHandler;
    private readonly IGetCustomerOrdersQueryHandler _getCustomerOrdersHandler;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IPlaceOrderCommandHandler placeOrderHandler,
        IGetOrderQueryHandler getOrderHandler,
        IGetCustomerOrdersQueryHandler getCustomerOrdersHandler,
        ILogger<OrdersController> logger)
    {
        _placeOrderHandler = placeOrderHandler;
        _getOrderHandler = getOrderHandler;
        _getCustomerOrdersHandler = getCustomerOrdersHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _placeOrderHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetOrder), new { orderId = result.Value.OrderId }, result.Value);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _getOrderHandler.HandleAsync(new GetOrderQuery(orderId), cancellationToken);

        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var orders = await _getCustomerOrdersHandler.HandleAsync(new GetCustomerOrdersQuery(customerId), cancellationToken);
        return Ok(orders);
    }
}