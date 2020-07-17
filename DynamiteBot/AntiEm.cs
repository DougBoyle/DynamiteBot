using System;
using System.Collections.Generic;
using System.Linq;
using BotInterface.Game;

namespace DynamiteBot {
    public class AntiEm : SmartBot {
        private int d = 100;
        
        public static List<Move> MovesRPS = new List<Move> {Move.R, Move.P, Move.S};

        public static Dictionary<Move, double> balance = new Dictionary<Move, double>() {
            {Move.R, 1.0}, {Move.P, 1.0}, {Move.S, 1.0}, {Move.D, 0.3}, {Move.W, 0}
        };
        
        public static Dictionary<Move, double> draw = new Dictionary<Move, double>() {
            {Move.R, 1.0}, {Move.P, 1.0}, {Move.S, 1.0}, {Move.D, 2.0}, {Move.W, 0}
        };
        
        public static Random random = new Random();

        public static Move Choose(List<Move> choices) {
            return choices[random.Next(choices.Count)];
        }
        
        public static Move ChooseWeighted(Dictionary<Move, double> choices) {
            var keys = new List<Move>();
            var vals = new List<double>();
            foreach (var entry in choices) {
                keys.Add(entry.Key);
                vals.Add(entry.Value);
            }

            return ChooseWeighted(keys, vals);
        }
        
        public static Move ChooseWeighted(List<Move> choices, List<double> probs) {
            var p = random.NextDouble();
            var norm = probs.Sum();
            for (int i = 0; i < choices.Count; i++) {
                if (p < probs[i] / norm) {
                    return choices[i];
                }

                p -= probs[i] / norm;
            }

            return choices[choices.Count - 1];
        }

        public void Update(Gamestate g) {
            var rounds = g.GetRounds();
            if (rounds.Length == 0) {
                return;
            }
            var round = rounds[rounds.Length - 1];
            var move1 = round.GetP1();
            if (move1 == Move.D) {d--;}
        }
        
        public Dictionary<Move, double> GetProbs(Gamestate g) {
            return new Dictionary<Move, double> {{MakeMove(g), 1.0}};
        }
        
        public Move MakeMove(Gamestate g) {
            Update(g);
            if (d > 0) {
                var r = g.GetRounds();
                var l = r.Length;
                Move move;
                if (l > 0 && r[l - 1].GetP2() == r[l - 1].GetP1()) {
                    move = ChooseWeighted(draw);
                }
                else {
                    move = ChooseWeighted(balance);
                }
                return move;
            }

            return Choose(MovesRPS);
        }
    }
}