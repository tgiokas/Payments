using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.Application.Dtos
{
    public record RegisterOrderResult(
        bool Success,
        string? GatewayOrderId,
        string? FormUrl,
        string? ErrorCode,
        string? ErrorMessage
    );
}
