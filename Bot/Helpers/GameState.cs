using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.ACNHFishing
{
    public enum GameState
    {
        Idle,
        Fetching,
        Active,
        Faulted,
        TimedOut
    }
}
