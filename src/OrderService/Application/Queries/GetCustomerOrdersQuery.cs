using OrderService.Domain;
using OrderService.Infrastructure;

namespace OrderService.Application.Queries;

public record GetCustomerOrdersQuery(Guid CustomerId);

public interface IGetCustomerOrdersQueryHandler
{
    Task<List<Order>> HandleAsync(GetCustomerOrdersQuery query, CancellationToken cancellationToken = default);
}

public class GetCustomerOrdersQueryHandler : IGetCustomerOrdersQueryHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetCustomerOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<Order>> HandleAsync(GetCustomerOrdersQuery query, CancellationToken cancellationToken = default)
    {
        return await _orderRepository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);
    }
}