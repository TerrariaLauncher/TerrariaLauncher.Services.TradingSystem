using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Command;

namespace TerrariaLauncher.Services.TradingSystem.Database.Commands
{
    public class RegisterGameUserCommand: Command
    {
        public int UserId { get; set; }
        public int GameUserId { get; set; }
    }
}
