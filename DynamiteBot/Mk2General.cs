using System;
using System.Collections.Generic;
using System.Linq;
using BotInterface.Bot;
using BotInterface.Game;

namespace DynamiteBot {
    public class Mk2General : SmartBot {
        
        public static Dictionary<Move, double> WinWeights(Dictionary<Move, double> moves) {
            var R = moves[Move.S] + moves[Move.W];
            var P = moves[Move.R] + moves[Move.W];
            var S = moves[Move.P] + moves[Move.W];
            var D = moves[Move.R] + moves[Move.P] + moves[Move.S]; // scale this by usage
            var W = 0; // scale this by usage

            var entropyScale = Utils.RPSEntropy(moves);
            D *= 0.5 + 1.5 * entropyScale; // half chance when predictable, double when no clue
            return new Dictionary<Move, double> {
                {Move.R,R}, {Move.P, P}, {Move.S, S}, {Move.D, D/25}, {Move.W, W}
            };
        }

        private double InstantMix = 0.5;
        private int cutOff = 1;

        public Dictionary<Move, double> DynamiteAdjust(Dictionary<Move, double> moves) {
            moves[Move.D] *= CurrentValue*CurrentValue;
            if (MyDynamite <= 0) {
                moves[Move.D] = 0;
            }
            return moves;
        }
        
        public int MyDynamite = 99; // opponent will always have chance of me playing dynamite
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
            var result = Utils.GetScore(move1, move2);
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
        }
        public  Dictionary<Move, double> initial = new Dictionary<Move, double> {
            {Move.R, 1.0}, {Move.P, 1.0}, {Move.S, 1.0}, {Move.D, 0.3}, {Move.W, 0}
        };
        
        public IMarkov model;
        public int order;
        
        
        
        public Mk2General(int i = 2) {
            order = i;
            model = new Mk2Markov(i);
            model.SetInitial(initial);
        }

        public Mk2General(IMarkov m, int i = 2) {
            order = i;
            model = m;
        }
        
        public Dictionary<Move, double> GetProbs(Gamestate g) {
            Update(g);
            var weights = model.GetProbs(g);
            var selectionWeights = DynamiteAdjust(WinWeights(weights));
            //var choice = GameLength < order+10 ? Choose(MovesRPS) : ChooseWeighted(selectionWeights);
            
            return GameLength < order + 5 ?
                model.GetInitial() : selectionWeights;
        }
        
        public Move MakeMove(Gamestate gamestate)
        {
            Update(gamestate);
            var weights = model.GetProbs(gamestate);
            var selectionWeights = DynamiteAdjust(WinWeights(weights));
            var choice = GameLength < order + 5 ?
                Utils.ChooseWeighted(DynamiteAdjust(model.GetInitial())) : 
                Utils.ChooseWeighted(selectionWeights);
            return choice;
        }
    }
}