﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.TradingSystem.Database.Commands
{
    public class DettachCharacterCommandAsync
    {
        public int UserId { get; set; }
        public int InstanceId { get; set; }
        public int InstanceUserId { get; set; }
    }
}
