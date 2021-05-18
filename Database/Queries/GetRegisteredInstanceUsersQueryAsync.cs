using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredInstanceUsersQueryAsync
    {
        public int UserId { get; set; }
        public int InstanceId { get; set; }
    }
}
