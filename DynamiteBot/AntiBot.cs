using System;
using System.Collections.Generic;
using System.Linq;
using BotInterface.Game;

namespace DynamiteBot {
    public class AntiBot :  SmartBot {

        public static Dictionary<Move, double> LoseWeights(Dictionary<Move, double> moves) {
            var R = moves[Move.P] + moves[Move.D] / 5 + moves[Move.W] / 10;
            var P = moves[Move.S] + moves[Move.D] / 5 + moves[Move.W] / 10;
            var S = moves[Move.R] + moves[Move.D] / 5 + moves[Move.W] / 10;
            var D = moves[Move.W] + (moves[Move.P] + moves[Move.S] + moves[Move.R])/10;
            var W = (moves[Move.P] + moves[Move.S] + moves[Move.R])/10;

            return new Dictionary<Move, double> {
                {Move.R,R}, {Move.P, P}, {Move.S, S}, {Move.D, D}, {Move.W, W}
            };
        }

        public double balance = 1.0;
        
        public Dictionary<Move, double> DynamiteAdjust(Dictionary<Move, double> moves) {
            var myFactor = MyDynamite / (1000.0 - MyScore) * Math.Sqrt(CurrentValue) * balance;
            moves[Move.D] *= myFactor;
            return moves;
        }
        
        public int MyDynamite = 99; // opponent will always have chance of me playing dynamite
        int MyScore = 0;
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
            var result = Utils.GetScore(move1, move2);
            if (result == 0) {
                CurrentValue++;
            }
            else {
                if (result > 0) {
                    MyScore += result*CurrentValue;
                }
                CurrentValue = 1;
            }
            
            selfDoubt.UpdateModel(g);
            dModel.Update(g);
        }
        
        public GeneralReverseMarkov selfDoubt;
        public DynamitePredictor dModel = new DynamitePredictor(0.1, 0.99);
        public int Order;
        
        public AntiBot(int order = 3) {
            selfDoubt = new GeneralReverseMarkov(order, 0.01);
            Order = order;
        }

        public Dictionary<Move, double> GetProbs(Gamestate g) {
            Update(g);
            var predicted = selfDoubt.GetProbs(g);

            return GameLength < Order + 5
                ? selfDoubt.GetInitial()
                : DynamiteAdjust(LoseWeights(predicted));
        }
        
        public Move MakeMove(Gamestate gamestate)
        {
            Update(gamestate);
            var predicted = selfDoubt.GetProbs(gamestate);

            var choice = GameLength < Order + 5
                ? Utils.ChooseWeighted(selfDoubt.GetInitial())
                : Utils.ChooseWeighted(DynamiteAdjust(LoseWeights(predicted)));
            return choice;
        }
    }
}