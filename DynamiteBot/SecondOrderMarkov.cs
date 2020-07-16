using System.Collections.Generic;
using System.Linq;
using BotInterface.Game;

namespace DynamiteBot {
    public class SecondOrderMarkov :IMarkov {
        public Dictionary<Move, double>[] Matrix = new Dictionary<Move, double>[625];

        public static Dictionary<Move,int> MoveToInt = new Dictionary<Move, int>()
            {{Move.R, 0}, {Move.P, 1}, {Move.S, 2}, {Move.D, 3}, {Move.W, 4}};
        
        public int GetIndex(Round r1, Round r2) {
            return  MoveToInt[r1.GetP1()] + MoveToInt[r1.GetP2()]*5 + 
                    MoveToInt[r2.GetP1()]*25 + MoveToInt[r2.GetP2()]*125;
        }

        public SecondOrderMarkov(double smooth = 0.2) {
            for (var i = 0; i < 625; i++) {
                Matrix[i] = new Dictionary<Move, double>();
                foreach (var move in Program.Moves) {
                    Matrix[i][move] = smooth;
                }
            }
        }
        
        public void UpdateModel(Gamestate gamestate) {
            var rounds = gamestate.GetRounds();
            if (rounds.Length <= 2) return;
            var r1 = rounds[rounds.Length - 3];
            var r2 = rounds[rounds.Length - 2];
            var latest = rounds[rounds.Length - 1];
            Matrix[GetIndex(r1, r2)][latest.GetP2()] += 1;
        }

        public Dictionary<Move, double> GetProbs(Gamestate gamestate) {
            var rounds = gamestate.GetRounds();
            if (rounds.Length < 2) return Program.Moves.ToDictionary(x => x, x => 1.0);
            var r1 = rounds[rounds.Length - 2];
            var r2 = rounds[rounds.Length - 1];
            return Matrix[GetIndex(r1,r2)];
        }
    }
}