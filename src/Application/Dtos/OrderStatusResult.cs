using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.Application.Dtos
{
    public record OrderStatusResult(
        bool Success,
        int? OrderStatus,     // 2 success per JCC docs
        string? ActionCode,
        string? ErrorCode,
        string? ErrorMessage
    );
}
