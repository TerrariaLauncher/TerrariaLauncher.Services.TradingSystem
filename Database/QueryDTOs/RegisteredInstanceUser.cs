using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.TradingSystem.Database.QueryDTOs
{
    public class RegisteredInstanceUser
    {
        public int InstanceId { get; set; }
        public int InstanceUserId { get; set; }
        public int UserId { get; set; }
    }
}
