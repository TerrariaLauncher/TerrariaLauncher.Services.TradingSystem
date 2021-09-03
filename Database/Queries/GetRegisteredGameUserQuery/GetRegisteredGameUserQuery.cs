using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredGameUserQuery: Query
    {
        public int Id { get; set; }
        public bool ByPassDeleted { get; set; }
    }
}
