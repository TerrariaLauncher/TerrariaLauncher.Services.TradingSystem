using System;
using System.Linq;
using TerrariaLauncher.Commons.Database.CQS.Query;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredGameUsersQuery : Query
    {
        public int UserId { get; set; }
        public bool ByPassDeleted { get; set; }
    }
}
