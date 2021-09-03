using System.Collections.Generic;
using TerrariaLauncher.Commons.Database.CQS.Request;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredGameUsersResult : IResult
    {
        public class RegisteredGameUser
        {
            public int GameUserId { get; set; }
            public bool Deleted { get; set; }
        }

        public int UserId { get; set; }
        public IList<RegisteredGameUser> RegisteredGameUsers { get; set; }
    }
}
