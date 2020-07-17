using System;
using System.Collections.Generic;
using BotInterface.Game;

namespace DynamiteBot {
    public interface IMarkov {
        void UpdateModel(Gamestate gamestate);
        Dictionary<Move, double> GetProbs(Gamestate gamestate);
        Dictionary<Move, double> GetInitial();
        void SetInitial(Dictionary<Move, double> m);
    }
}