using BotInterface.Game;

namespace DynamiteBot {
    public class DynamitePredictor {
        private double D;
        private double Other;
        private double DecayRate;

        public DynamitePredictor(double smooth = 0.1, double decayRate = 0.99) {
            D = smooth;
            Other = smooth*10;
            DecayRate = decayRate;
        }

        public void Update(Gamestate g) {
            var rounds = g.GetRounds();
            if (rounds.Length > 0) {
                D *= DecayRate;
                Other *= DecayRate;
                if (rounds[rounds.Length - 1].GetP2() == Move.D) {
                    D++;
                }
                else {
                    Other++;
                }
            }
        }

        public double GetEstimate() {
            return D / (D + Other);
        }
    }
}