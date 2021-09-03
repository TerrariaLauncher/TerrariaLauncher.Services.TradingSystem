using TerrariaLauncher.Commons.Database.CQS.Request;

namespace TerrariaLauncher.Services.TradingSystem.Database.Queries
{
    public class GetRegisteredGameUserResult: IResult
    {
        public int UserId { get; set; }
        public int GameUserId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
