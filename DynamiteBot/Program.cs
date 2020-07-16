using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BotInterface.Bot;
using BotInterface.Game;

namespace DynamiteBot {
    public class Program : IBot {
        
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
        public Move ChooseWeighted(List<Move> choices, List<double> probs) {
            var p = random.NextDouble();
            var norm = probs.Sum();
            for (int i = 0; i < choices.Count; i++) {
                if (p < probs[i] / norm) {
                    return choices[i];
                }
                p -= probs[i] / norm;
            }
            return choices[choices.Count-1];
        }

        public Move ChooseWeighted(Dictionary<Move, double> choices) {
            var keys = new List<Move>();
            var vals = new List<double>();
            foreach (var entry in choices) {
                keys.Add(entry.Key);
                vals.Add(entry.Value);
            }
            return ChooseWeighted(keys, vals);
        }
        
        public static double MaxH = Math.Log(3.0);
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
        
        public static Dictionary<Move, double> WinWeights(Dictionary<Move, double> moves) {
            var R = moves[Move.S] + moves[Move.W] / 3;
            var P = moves[Move.R] + moves[Move.W] / 3;
            var S = moves[Move.P] + moves[Move.W] / 3;
            var D = moves[Move.R] + moves[Move.P] + moves[Move.S]; 
            var W = moves[Move.D];

            var entropyScale = RPSEntropy(moves);
            D *= 0.5 + 1.5 * entropyScale; 
            
            return new Dictionary<Move, double> {
                {Move.R,R}, {Move.P, P}, {Move.S, S}, {Move.D, D/3}, {Move.W, W}
            };
        }

        private double InstantMix = 0.5;
        private int cutOff = 1;

        private double AdjustTheirDynamite(double rate) {
            var instantRate = dModel.GetEstimate();
            var theirFactor = (TheirDynamite * 10.0) / (1000.0 - TheirScore);

            rate = rate * (1 - InstantMix) + instantRate * InstantMix;
            if (TheirDynamite <= cutOff) {
                rate *= theirFactor;
            }
            return rate;
        }
        
        public Dictionary<Move, double> DynamiteAdjust(Dictionary<Move, double> moves) {
            var myFactor = MyDynamite * 10.0 / (1000.0 - MyScore) * Math.Sqrt(CurrentValue) * 0.7;
            moves[Move.D] *= myFactor;
            return moves;
        }
        
        int MyDynamite = 99; 
        int TheirDynamite = 100;
        int MyScore = 0;
        int TheirScore = 0;
        int CurrentValue = 1;
        private int GameLength = 0;

        public void Update(Gamestate g) {
            var rounds = g.GetRounds();
            GameLength++;
            if (rounds.Length == 0) {
                return;
            }
            var round = rounds[rounds.Length - 1];
            var move1 = round.GetP1();
            var move2 = round.GetP2();
            if (move1 == Move.D) {MyDynamite--;}
            if (move2 == Move.D) {TheirDynamite--;}
            var result = GetScore(move1, move2);
            if (result == 0) {
                CurrentValue++;
            }
            else {
                if (result > 0) {
                    MyScore += result*CurrentValue;
                } else {
                    TheirScore -= result*CurrentValue;
                }
                CurrentValue = 1;
            }
            
            model.UpdateModel(g);
            dModel.Update(g);
        }
        
        public IMarkov model = new SecondOrderMarkov(0.01);
        public DynamitePredictor dModel = new DynamitePredictor(0.1, 0.99);
        
        public Move MakeMove(Gamestate gamestate)
        {
            Update(gamestate);
            var weights = model.GetProbs(gamestate);
            var tot = weights.Values.Sum();
            var dRate = AdjustTheirDynamite(weights[Move.D] / tot);
            weights[Move.D] = tot * dRate;
            var selectionWeights = DynamiteAdjust(WinWeights(weights));
            var choice = GameLength < 10 ? Choose(MovesRPS) : ChooseWeighted(selectionWeights);
            return choice;
        }
    }
}