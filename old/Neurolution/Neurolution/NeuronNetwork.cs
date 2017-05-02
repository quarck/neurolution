using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Neurolution
{
    // specialization types: 
    // Light
    // Current Energy 
    // Smell 

    [Serializable]
    public class LightSensitivityParam
    {
        public bool Sensetive = false;
        public double Direction = 0.0; // radians 
        public double Width = 0.0; // radians 

        [NonSerialized]
        public int InputIdx = -1;

        public static LightSensitivityParam CloneFrom(LightSensitivityParam other, Random rnd)
        {
            double maxMutation = AppProperties.NetworkMaxRegularMutation;

            return
                new LightSensitivityParam
                {
                    Sensetive = other.Sensetive,
                    Direction = other.Direction + (2.0 * rnd.NextDouble() - 1.0) * maxMutation, 
                    Width = Math.Max(0.001, other.Width  + (2.0 * rnd.NextDouble() - 1.0) * maxMutation)
                };
        }

        public static LightSensitivityParam CloneFromWithSevereRandom(LightSensitivityParam other, Random rnd)
        {
            double alpha = AppProperties.NetworkSevereMutationAlpha;
            double sensitivityProbability = AppProperties.LightSensetivityProbability;

            return
                new LightSensitivityParam
                {
                    Sensetive = rnd.NextDouble() < sensitivityProbability,
                    Direction = other.Direction * alpha + (2.0 * rnd.NextDouble() - 1.0) * (1-alpha),
                    Width = Math.Max(0.001, Math.Abs(other.Width * alpha + (2.0 * rnd.NextDouble() - 1.0) * (1-alpha)))
                };
        }
    }

    [Serializable]
    public class Neuron
    {
        public double[] Weights;

        public double BaseLevel;

        public LightSensitivityParam LightSensitivity;

        public Neuron(int size, Random rnd)
        {
            Weights = new double[size];

            BaseLevel = 0.0;

            for (int i = 0; i < size; ++i)
                Weights[i] = 0.0;

            LightSensitivity = new LightSensitivityParam();
        }

        public Neuron()
        {
            Weights = null;
            BaseLevel = 0;
        }

        public static Neuron CloneFrom(Neuron other, Random rnd)
        {
            double maxMutation = AppProperties.NetworkMaxRegularMutation;

            return
                new Neuron
                {
                    Weights =
                        other.Weights
                        .Select(x => x + (2.0 * rnd.NextDouble() - 1.0) * maxMutation)
                        .ToArray(),

                    BaseLevel = other.BaseLevel + (2.0 * rnd.NextDouble() - 1.0) * maxMutation,

                    LightSensitivity = LightSensitivityParam.CloneFrom(other.LightSensitivity, rnd)
                };
        }

        public static Neuron CloneFromWithSevereRandom(Neuron other, Random rnd)
        {
            double alpha = AppProperties.NetworkSevereMutationAlpha;

            return 
                new Neuron
                {
                    Weights =
                        other.Weights
                        .Select(x => x * alpha + (2.0 * rnd.NextDouble() - 1.0) * (1.0 - alpha))
                        .ToArray(),

                    BaseLevel = other.BaseLevel * alpha + (2.0 * rnd.NextDouble() - 1.0), 

                    LightSensitivity = LightSensitivityParam.CloneFromWithSevereRandom(other.LightSensitivity, rnd)
                };
        }

        public static Neuron CloneFromWithShrink(Neuron other, Random rnd, bool[] keepVector)
        {
            return
                new Neuron
                {
                    Weights = other.Weights
                        .Where((v, idx) => keepVector[idx])
                        .ToArray(),

                    BaseLevel = other.BaseLevel,

                    LightSensitivity = LightSensitivityParam.CloneFrom(other.LightSensitivity, rnd)
                };            
        }

        public static Neuron CloneFromWithExpansion(Neuron other, Random rnd, bool[] doubleVector)
        {
            double alpha = AppProperties.NetworkSevereMutationAlpha;

            var mainValues = other.Weights;

            var newValues = other.Weights
                .Where((v, idx) => doubleVector[idx])
                .Select(x => x * alpha + (2.0 * rnd.NextDouble() - 1.0) * (1.0 - alpha));

            return 
                new Neuron
                {
                    Weights = mainValues.Concat(newValues).ToArray(),
                    BaseLevel = other.BaseLevel,
                    LightSensitivity = LightSensitivityParam.CloneFromWithSevereRandom(other.LightSensitivity, rnd)
                };
        }

        public static Neuron CloneFromWithExpansionZeroValues(Neuron other, Random rnd, bool[] doubleVector)
        {
            double maxMutation = AppProperties.NetworkMaxRegularMutation;

            var mainValues = other.Weights;

            var newValues = other.Weights
                .Where((v, idx) => doubleVector[idx])
                .Select(x =>  (2.0 * rnd.NextDouble() - 1.0) * maxMutation );

            return
                new Neuron
                {
                    Weights = mainValues.Concat(newValues).ToArray(),
                    BaseLevel = other.BaseLevel,
                    LightSensitivity = LightSensitivityParam.CloneFromWithSevereRandom(other.LightSensitivity, rnd)
                };
        }
    }

    [Serializable]
    public class NeuronNetwork
    {
        public Neuron[] Neurons;

        [NonSerialized]
        public Neuron[] Eye = new Neuron[0]; // light-sensetive sub-set of Neurons

        public double[] InputVector;

        public double[] OutputVector;

        private int Size => Neurons.Length;

        public double EnergeyPerIteration => 
            Size < 32 
                ? 32 * AppProperties.EnergyConsumptionPerNeuron
                : Size * AppProperties.EnergyConsumptionPerNeuron;

        public NeuronNetwork(int size, Random rnd)
        {
            Neurons = new Neuron[size];
            for (int i = 0; i < size; ++i)
            {
                Neurons[i] = new Neuron(size, rnd);
            }

            InputVector = new double[size];
            OutputVector = new double[size];

            if (rnd != null)
            {
                for (int i = 0; i < size; ++i)
                {
                    Neurons[i].LightSensitivity.Direction = Math.PI * (2.0 * rnd.NextDouble() - 1.0);
                    Neurons[i].LightSensitivity.Width = Math.PI * rnd.NextDouble();
                }
            }
        }

        public NeuronNetwork()
        {
        }

        private void IterateNetwork(Random rnd, double[] inputVector, double[] outputVector)
        {
            for (int j = 0; j < Neurons.Length; ++j)
            {
                var neuron = Neurons[j];

                double weightedInput = neuron.BaseLevel - neuron.Weights[j] * inputVector[j];

                for (int i = 0; i < Neurons.Length; ++i)
                {
                    weightedInput += neuron.Weights[i] * inputVector[i];
                }

                // add some noise 
                weightedInput += (2.0 * rnd.NextDouble() - 1.0) * AppProperties.NetworkNoiseLevel;

                outputVector[j] = weightedInput.ShiftedSigmoid();
            }
        }


        public void PrepareIteration()
        {
            var prevInput = InputVector;
            InputVector = OutputVector;
            OutputVector = prevInput;
        }

        public void IterateNetwork(Random rnd)
        {
            IterateNetwork(rnd, InputVector, OutputVector);
        }

        public void UpdateEye()
        {
            Eye = Neurons
                .Where((x, idx) =>
                {
                    x.LightSensitivity.InputIdx = idx;
                    return (x.LightSensitivity.Sensetive) && (idx > AppProperties.NetworkLastSpecialIdx);
                })
                .ToArray();
        }

        public void CleanOutputs()
        {
            for (int i = 0; i < Size; ++i)
            {
                InputVector[i] = 0.0;
                OutputVector[i] = 0.0;
            }
        }

        private void CloneRegular(NeuronNetwork other, Random rnd)
        {
            Neurons = other.Neurons
                .Select(x => Neuron.CloneFrom(x, rnd))
                .ToArray();

            UpdateEye();

            InputVector = other.InputVector.Select(x => x).ToArray();
            OutputVector = other.OutputVector.Select(x => x).ToArray();
        }

        private void CloneSevereRandomValues(NeuronNetwork other, Random rnd, double severity)
        {
            Neurons = other.Neurons
                .Select(x =>
                    (rnd.NextDouble() < severity) 
                        ? Neuron.CloneFromWithSevereRandom(x, rnd) 
                        : Neuron.CloneFrom(x, rnd))
                .ToArray();

            UpdateEye();

            InputVector = other.InputVector.Select(x => x).ToArray();
            OutputVector = other.OutputVector.Select(x => x).ToArray();
        }

        private void CloneSevereShrinkNetwork(NeuronNetwork other, Random rnd, double severity)
        {
            Debug.Assert(other.Neurons.Length == other.InputVector.Length);
            Debug.Assert(other.Neurons.Length == other.OutputVector.Length);

            bool[] keepVector =
                other.Neurons
                    .Select(x => rnd.NextDouble() >= severity)
                    .ToArray();

            if (keepVector.Count(keep => keep) < AppProperties.MinNetworkSize)
            {
                CloneSevereRandomValues(other, rnd, severity); // failback
                return;
            }

            Neurons = other.Neurons
                .Where((n, idx) => keepVector[idx])
                .Select(n => Neuron.CloneFromWithShrink(n, rnd, keepVector))
                .ToArray();

            UpdateEye();

            InputVector = other.InputVector.Where((x, idx) => keepVector[idx]).ToArray();
            OutputVector = other.OutputVector.Where((x, idx) => keepVector[idx]).ToArray();

            Debug.Assert(Neurons.Length == InputVector.Length);
            Debug.Assert(Neurons.Length == OutputVector.Length);
#if DEBUG
            foreach (var neuron in Neurons)
            {
                Debug.Assert(neuron.Weights.Length == Neurons.Length);
            }
#endif
        }

        private void CloneSevereExtendNetwork(NeuronNetwork other, Random rnd, double severity)
        {
            Debug.Assert(other.Neurons.Length == other.InputVector.Length);
            Debug.Assert(other.Neurons.Length == other.OutputVector.Length);

            bool[] doubleVector =
                other.Neurons
                    .Select(x => rnd.NextDouble() >= severity)
                    .ToArray();

            if (other.Size + doubleVector.Count(dbl => dbl) > AppProperties.MaxNetworkSize)
            {
                CloneSevereRandomValues(other, rnd, severity); // failback
                return;
            }

            Neurons =
                other.Neurons
                    .Concat( other.Neurons.Where((n, idx) => doubleVector[idx]) )
                    .Select( n => Neuron.CloneFromWithExpansion(n, rnd, doubleVector) )
                    .ToArray();

            UpdateEye();

            InputVector = other.InputVector.Concat(other.InputVector.Where((x, idx) => doubleVector[idx])).ToArray();
            OutputVector = other.OutputVector.Concat(other.OutputVector.Where((x, idx) => doubleVector[idx])).ToArray();

            Debug.Assert(Neurons.Length == InputVector.Length);
            Debug.Assert(Neurons.Length == OutputVector.Length);
#if DEBUG
            foreach (var neuron in Neurons)
            {
                Debug.Assert(neuron.Weights.Length == Neurons.Length);
            }
#endif
        }

        private void CloneSevereExtendNetworkZeroLevel(NeuronNetwork other, Random rnd, double severity)
        {
            Debug.Assert(other.Neurons.Length == other.InputVector.Length);
            Debug.Assert(other.Neurons.Length == other.OutputVector.Length);

            bool[] doubleVector =
                other.Neurons
                    .Select(x => rnd.NextDouble() >= severity)
                    .ToArray();

            if (other.Size + doubleVector.Count(dbl => dbl) > AppProperties.MaxNetworkSize)
            {
                CloneSevereRandomValues(other, rnd, severity); // failback
                return;
            }

            Neurons =
                other.Neurons
                    .Concat(other.Neurons.Where((n, idx) => doubleVector[idx]))
                    .Select(n => Neuron.CloneFromWithExpansionZeroValues(n, rnd, doubleVector))
                    .ToArray();

            UpdateEye();

            InputVector = other.InputVector.Concat(other.InputVector.Where((x, idx) => doubleVector[idx])).ToArray();
            OutputVector = other.OutputVector.Concat(other.OutputVector.Where((x, idx) => doubleVector[idx])).ToArray();

            Debug.Assert(Neurons.Length == InputVector.Length);
            Debug.Assert(Neurons.Length == OutputVector.Length);
#if DEBUG
            foreach (var neuron in Neurons)
            {
                Debug.Assert(neuron.Weights.Length == Neurons.Length);
            }
#endif
        }

        public void CloneFrom(NeuronNetwork other, Random rnd, bool severeMutations, double severity)
        {
            if (!severeMutations)
            {
                CloneRegular(other, rnd);
            }
            else
            {
                CloneSevereRandomValues(other, rnd, severity);
                switch (rnd.Next(4))
                {
                    case 0:
                        CloneSevereRandomValues(other, rnd, severity);
                        break;
                    case 1:
                        CloneSevereShrinkNetwork(other, rnd, severity);
                        break;
                    case 2:
                        CloneSevereExtendNetwork(other, rnd, severity);
                        break;
                    case 3:
                        CloneSevereExtendNetworkZeroLevel(other, rnd, severity);
                        break;
                }
            }
        }
    }
}
