using System;
using System.Collections.Generic;
using System.Linq;
using BotInterface.Game;

namespace DynamiteBot {
    public class Utils {
        private static Dictionary<Move, Dictionary<Move, int>> outcomes = new Dictionary<Move, Dictionary<Move, int>> {
            {Move.R, new Dictionary<Move, int> {{Move.R, 0}, {Move.P, -1}, {Move.S, 1}, {Move.D, -1}, {Move.W, 1}}},
            {Move.P, new Dictionary<Move, int> {{Move.R, 1}, {Move.P, 0}, {Move.S, -1}, {Move.D, -1}, {Move.W, 1}}},
            {Move.S, new Dictionary<Move, int> {{Move.R, -1}, {Move.P, 1}, {Move.S, 0}, {Move.D, -1}, {Move.W, 1}}},
            {Move.D, new Dictionary<Move, int> {{Move.R, 1}, {Move.P, 1}, {Move.S, 1}, {Move.D, 0}, {Move.W, -1}}},
            {Move.W, new Dictionary<Move, int> {{Move.R, -1}, {Move.P, -1}, {Move.S, -1}, {Move.D, 1}, {Move.W, 0}}}
        };

        public static void PrintPredictions(Dictionary<Move, double> moves) {
            foreach (var move in moves) {
                Console.Write($"{move.Key} = {move.Value.ToString("n2")}, ");
            }

            Console.WriteLine();
        }

        public static int GetScore(Move m1, Move m2) {
            return outcomes[m1][m2];
        }

        public static List<Move> Moves = Enum.GetValues(typeof(Move)).Cast<Move>().ToList();
        public static List<Move> MovesRPS = new List<Move> {Move.R, Move.P, Move.S};

        public static bool RoundsEqual(Round a, Round b) {
            return a.GetP1() == b.GetP1() && a.GetP2() == b.GetP2();
        }

        public static bool RoundsEqualForOpponent(Round a, Round b) {
            return a.GetP2() == b.GetP2();
        }

        public static bool RangeEqual(Gamestate g, int start, int shouldEqualStart, int length) {
            Round[] rounds = g.GetRounds();
            for (int i = 0; i < length; i++) {
                if (!RoundsEqual(rounds[start + i], rounds[shouldEqualStart + i])) {
                    return false;
                }
            }

            return true;
        }

        public static Random random = new Random();

        public static Move Choose(List<Move> choices) {
            return choices[random.Next(choices.Count)];
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
        
        public static Dictionary<Move, double> Norm(Dictionary<Move, double> moves) {
            var total = moves.Values.Sum();
            var result = new Dictionary<Move, double>();
            foreach (var x in moves) {
                result[x.Key] = x.Value / total;
            }
            return result;
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
        
        public static double MaxH = Math.Log(3.0);
        // should care only about RPS since if decision is between R/D or R/W or D/W,
        // P best or have choice between P and D.
        public static double RPSEntropy(Dictionary<Move, double> moves) {
            double R = moves[Move.R];
            double P = moves[Move.P];
            double S = moves[Move.S];
            double tot = R + P + S;
            R /= tot;
            P /= tot;
            S /= tot;
            double H = -(R * Math.Log(R) + P * Math.Log(P) + S * Math.Log(S));
            return H / MaxH;
        }
    }
}