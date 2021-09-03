using System;
using System.Collections.Generic;
using System.Linq;
using TerrariaLauncher.Commons.Database.CQS.Command;

namespace TerrariaLauncher.Services.TradingSystem.Database.Commands
{
    public class DeregisterGameUserCommand : Command
    {
        public int UserId { get; set; }
        public int GameUserId { get; set; }
    }
}
