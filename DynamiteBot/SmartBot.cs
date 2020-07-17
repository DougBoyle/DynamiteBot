using System.Collections.Generic;
using BotInterface.Game;

namespace DynamiteBot {
    public interface SmartBot {
        Move MakeMove(Gamestate g);
        Dictionary<Move, double> GetProbs(Gamestate g);
    }
}