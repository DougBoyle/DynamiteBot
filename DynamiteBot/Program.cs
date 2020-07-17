using System;
using System.Collections.Generic;
using BotInterface.Bot;
using BotInterface.Game;

namespace DynamiteBot {
    public class Program : IBot, SmartBot {
        private List<SmartBot> Bots;
        private List<double> Scores; // can have falling off weighting
        private List<Dictionary<Move,double>> Predictions;
        private double Decay;

        private bool DrawVotes;
        private bool Exp;
        
        
        public int CurrentValue = 1;
        
        public Program() {
            Bots = new List<SmartBot> {
                new ProgramGeneral(3), new ProgramGeneral(2), new ProgramGeneral(1),
                new AntiBot(3), new AntiBot(2), new AntiBot(1),
                new AntiEm(), new Mk2General(1), new Mk2General(2), new Mk2General(3)
            };
            Scores = new List<double>();
            Scores = new List<double> {
                5, 0, 0,
                2, 0, 0,
                10, 0, 0, 0
            };
            Decay = 0.99;
            DrawVotes = true;
            Exp = true;
        }

        public void Update(Gamestate g) {
            var rounds = g.GetRounds();
            if (rounds.Length == 0) {
                return;
            }
            var round = rounds[rounds.Length - 1];
            var move1 = round.GetP1();
            var move2 = round.GetP2();
            int result;
            
            for (var i = 0; i < Bots.Count; i++) {
                var moves = Predictions[i];
                foreach (var m in moves) {
                    if (Exp) {
                        result = Utils.GetScore(m.Key, move2);
                    }
                    else {
                        result = Math.Max(Utils.GetScore(m.Key, move2), 0);
                    }

                    Scores[i] = Scores[i] * Decay + result * CurrentValue * m.Value;
                }
            }
            result = Utils.GetScore(move1, move2);
            if (result == 0) {
                CurrentValue++;
            }
            else {
                CurrentValue = 1;
            }
        }
        
        public Dictionary<Move, double> GetProbs(Gamestate g) {
            Update(g);
            Dictionary<Move, double> votes = new Dictionary<Move, double>();
            foreach (var move in Utils.Moves) {
                votes[move] = 0.0;
            }
            Predictions = new List<Dictionary<Move, double>>();
            for (int i = 0; i < Bots.Count; i++) {
                // Could add each bot's distribution instead and do full Baysian
                var moves = Bots[i].GetProbs(g);
                Predictions.Add(moves);
                foreach (var m in moves) {
                    if (Exp) {
                        votes[m.Key] += m.Value * Math.Exp(Scores[i] / 8.0);
                    }
                    else {
                        votes[m.Key] += m.Value * Scores[i];
                    }
                }
            }
            return votes;
        }

        public Move MakeMove(Gamestate g) {
            Update(g);
            Dictionary<Move, double> votes = new Dictionary<Move, double>();
            foreach (var move in Utils.Moves) {
                votes[move] = 0.0;
            }
            Predictions = new List<Dictionary<Move, double>>();
            for (int i = 0; i < Bots.Count; i++) {
                // Could add each bot's distribution instead and do full Baysian
                var moves = Bots[i].GetProbs(g);
                Predictions.Add(moves);
                foreach (var m in moves) {
                    if (Exp) {
                        votes[m.Key] += m.Value * Math.Exp(Scores[i] / 8.0);
                    }
                    else {
                        votes[m.Key] += m.Value * Scores[i];
                    }
                }
            }

            // could select from distribution at this point
            if (DrawVotes) {
                return Utils.ChooseWeighted(votes);
            }
            var best = double.MinValue;
            var bestMove = Move.R;
            foreach (var vote in votes) {
                if (vote.Value > best) {
                    best = vote.Value;
                    bestMove = vote.Key;
                }
            }
            return bestMove;
        }
    }
}
    