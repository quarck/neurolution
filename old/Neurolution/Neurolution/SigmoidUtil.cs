using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neurolution
{
    public static class SigmoidUtil
    {
        public static double Sigmoid(this double value)
        {
            return 1.0 / (1.0 + Math.Exp(-value));
        }

        public static double ShiftedSigmoid(this double value)
        {
            return 1.0 / (1.0 + Math.Exp(-value)) - 0.5;
        }
    }
}
