using System;
using System.Collections.Generic;
using BotInterface.Game;

namespace DynamiteBot {
    public class GeneralReverseMarkov {
        private Dictionary<int, Dictionary<Move, double>> Matrix = new Dictionary<int, Dictionary<Move, double>>();

        private int Order;
        private double Smooth;

        public static Dictionary<Move, int> MoveToInt = new Dictionary<Move, int>()
            {{Move.R, 0}, {Move.P, 1}, {Move.S, 2}, {Move.D, 3}, {Move.W, 4}};

        public int GetIndex(IEnumerable<Round> rounds) {
            int index = 0;
            foreach (var round in rounds) {
                index *= 25;
                index += MoveToInt[round.GetP1()] + MoveToInt[round.GetP2()] * 5;
            }
            return index;
        }

        public GeneralReverseMarkov(int order = 3, double smooth = 0.01) {
            Order = order;
            Smooth = smooth;
        }

        public void UpdateModel(Gamestate gamestate) {
            var rounds = gamestate.GetRounds();
            if (rounds.Length <= Order) return;
            var segment = new ArraySegment<Round>(rounds, rounds.Length - Order - 1, Order);
            var latest = rounds[rounds.Length - 1];
            var index = GetIndex(segment);
            if (!Matrix.ContainsKey(index)) {
                Matrix[index] = new Dictionary<Move, double>();
                foreach (var move in Utils.Moves) {
                    Matrix[index][move] = Smooth;
                }
            }

            Matrix[index][latest.GetP1()] += 1;
        }

        public static Dictionary<Move, double> initial = new Dictionary<Move, double> {
            {Move.R, 1.0}, {Move.P, 1.0}, {Move.S, 1.0}, {Move.D, 1.0}, {Move.W, 1.0}
        };
        public Dictionary<Move, double> GetInitial() {
            return new Dictionary<Move, double>(initial);
        }
        public void SetInitial(Dictionary<Move, double> m) {
            initial = m;
        }

        // TODO: Need to handle if no information
        // Could have a threshold detail level and fall back to lower order if needed
        // Detail level could be based on highest X counts in table
        public Dictionary<Move, double> GetProbs(Gamestate gamestate) {
            var rounds = gamestate.GetRounds();
            if (rounds.Length < Order) return initial;
            //  var segment = new ArraySegment<Round>(rounds, rounds.Length - Order, Order);
            var segment = new ArraySegment<Round>(rounds, rounds.Length - Order, Order);
            var index = GetIndex(segment);
            if (!Matrix.ContainsKey(index)) {
                Matrix[index] = new Dictionary<Move, double>();
                foreach (var move in Utils.Moves) {
                    Matrix[index][move] = Smooth;
                }
            }

            return new Dictionary<Move, double>(Matrix[index]);
        }
    }
}