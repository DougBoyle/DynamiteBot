using System;
using System.Collections.Generic;
using BotInterface.Game;

namespace DynamiteBot {
    public class ReverseMarkov {
        private Dictionary<int, Dictionary<Move,double>> Matrix = new Dictionary<int, Dictionary<Move, double>>();
        //private int Order;
        private double Smooth;

        public static Dictionary<Move,int> MoveToInt = new Dictionary<Move, int>()
            {{Move.R, 0}, {Move.P, 1}, {Move.S, 2}, {Move.D, 3}, {Move.W, 4}};
        
        public int GetIndex(Round round) {
            return MoveToInt[round.GetP1()] + MoveToInt[round.GetP2()]*5;
        }

        public ReverseMarkov(double smooth = 0.01) {
        //    Order = order;
        Smooth = smooth;
        }
        
        public void UpdateModel(Gamestate gamestate) {
            var rounds = gamestate.GetRounds();
            if (rounds.Length < 2) return;
            var prev = rounds[rounds.Length - 2];
            var latest = rounds[rounds.Length - 1];
            var index = GetIndex(prev);
            if (!Matrix.ContainsKey(index)) {
                Matrix[index] = new Dictionary<Move, double>();
                foreach (var move in Program.Moves) {
                    Matrix[index][move] = Smooth;
                }
            }
            Matrix[index][latest.GetP1()] += 1;
        }
        
        public static Dictionary<Move, double> initial = new Dictionary<Move, double> {
            {Move.R, 1.0}, {Move.P, 1.0}, {Move.S, 1.0}, {Move.D, 1.0}, {Move.W, 1.0}
        };

        // TODO: Need to handle if no information
        // Could have a threshold detail level and fall back to lower order if needed
        // Detail level could be based on highest X counts in table
        public Dictionary<Move, double> GetProbs(Gamestate gamestate) {
            var rounds = gamestate.GetRounds();
            if (rounds.Length == 0) return initial;
           //  var segment = new ArraySegment<Round>(rounds, rounds.Length - Order, Order);
            var latest = rounds[rounds.Length - 1];
            var index = GetIndex(latest);
            if (!Matrix.ContainsKey(index)) {
                Matrix[index] = new Dictionary<Move, double>();
                foreach (var move in Program.Moves) {
                    Matrix[index][move] = Smooth;
                }
            }
            return new Dictionary<Move, double>(Matrix[index]);
        }
    }
}