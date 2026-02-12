using OrderService.Domain;
using OrderService.Infrastructure;
using SharedKernel.Models;

namespace OrderService.Application.Queries;

public record GetOrderQuery(Guid OrderId);

public interface IGetOrderQueryHandler
{
    Task<Result<Order>> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default);
}

public class GetOrderQueryHandler : IGetOrderQueryHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<Order>> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order == null)
            return Result.Failure<Order>($"Order {query.OrderId} not found");

        return Result.Success(order);
    }
}