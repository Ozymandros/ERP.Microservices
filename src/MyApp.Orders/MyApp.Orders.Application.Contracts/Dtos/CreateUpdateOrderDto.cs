using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    public record CreateUpdateOrderDto(
        [property: Required]
        string OrderNumber,
        
        [property: Required]
        Guid CustomerId,
        
        [property: Required]
        DateTime OrderDate,
        
        [property: Required]
        List<OrderLineDto> Lines
    );
}
