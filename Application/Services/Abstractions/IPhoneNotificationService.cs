using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Abstractions
{
    public interface IPhoneNotificationService
    {
        Task SendToUserAsync(string userId, string message);
        Task SendToAllAsync(string message);
    }
}
