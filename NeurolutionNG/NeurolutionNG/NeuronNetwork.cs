using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media.Animation;

namespace Neurolution
{
    // specialization types: 
    // Light
    // Current Energy 
    // Smell 

    public class LightSensor
    {
        public float Direction = 0.0f; // radians 
        public float Width = 0.0f;
        public bool SensetiveToRed = false;
    }

    public enum NeuronState: int
    {
        Idle, 
        Excited0,
        Excited1,
        Recovering0, 
        Recovering1
    }

    [Serializable]
    public class Neuron
    {
        public float[] Weights;

        [NonSerialized]    
        public float Charge = 0.0f;

        [NonSerialized]
        public NeuronState State = NeuronState.Idle;

        public Neuron(int size, Random rnd)
        {
            Weights = new float[size];

            for (int i = 0; i < size; ++i)
                Weights[i] = 0.0f;
        }

        public Neuron()
        {
            Weights = null;
        }

        public static Neuron CloneFrom(Neuron other, Random rnd)
        {
            float maxMutation = AppProperties.NetworkMaxRegularMutation;

            return
                new Neuron
                {
                    Weights =
                        other.Weights
                        .Select(x => (float)(x + (2.0 * rnd.NextDouble() - 1.0) * maxMutation))
                        .ToArray(),

                    Charge = 0.0f, 

                    State = NeuronState.Idle
                };
        }

        public static Neuron CloneFromWithSevereRandom(Neuron other, Random rnd)
        {
            float alpha = AppProperties.NetworkSevereMutationAlpha;

            return 
                new Neuron
                {
                    Weights =
                        other.Weights
                        .Select(x => (float)(x * alpha + (2.0 * rnd.NextDouble() - 1.0) * (1.0 - alpha)))
                        .ToArray(),

                    Charge = 0.0f,

                    State = NeuronState.Idle
                };
        }     
    }

    [Serializable]
    public class NeuronNetwork
    {
        public Neuron[] Neurons;

        [NonSerialized]
        public LightSensor[] Eye; 

        public float[] InputVector;

        public float[] OutputVector;

        private int NetworkSize => Neurons.Length;
        private int VectorSize => NetworkSize + AppProperties.EyeSize;

        public NeuronNetwork(int networkSize, Random rnd)
        {
            Eye = new LightSensor[AppProperties.EyeSize];

            for (int i = 0; i < AppProperties.EyeSize; ++i)
            {
                Eye[i] = new LightSensor();;
                double iPrime = ( (i >> 1) - AppProperties.EyeSize / 2) + 0.5;
                Eye[i].Direction = (float) (AppProperties.EyeCellDirectionStep * iPrime);
                Eye[i].Width = AppProperties.EyeCellWidth;
                Eye[i].SensetiveToRed = (i & 1) == 0;
            }

            Neurons = new Neuron[networkSize];
            for (int i = 0; i < networkSize; ++i)
            {
                Neurons[i] = new Neuron(AppProperties.EyeSize + networkSize, rnd);
            }

            InputVector = new float[VectorSize];
            OutputVector = new float[VectorSize];
        }

        public NeuronNetwork()
        {
        }

        private float ValueCap(float val, float min, float max)
        {
            return Math.Min(Math.Max(val, min), max);
        }

        private void IterateNetwork(Random rnd, float[] inputVector, float[] outputVector)
        {
            for (int j = 0; j < Neurons.Length; ++j)
            {
                int neuronPositionInInputVector = j + AppProperties.EyeSize;

                // 1st step - calculate updated charge values
                var neuron = Neurons[j];

                float weightedInput = -neuron.Weights[neuronPositionInInputVector] * inputVector[neuronPositionInInputVector];

                for (int i = 0; i < neuron.Weights.Length; ++i)
                {
                    weightedInput += neuron.Weights[i] * inputVector[i];
                }

                // add some noise 
                weightedInput += (float)((2.0 * rnd.NextDouble() - 1.0) * AppProperties.NetworkNoiseLevel);


                // 2nd step - update neuron state according to current state + new input
                // and update state and current output

                switch (neuron.State)
                {
                    case NeuronState.Idle:

                        neuron.Charge = ValueCap(
                            neuron.Charge * AppProperties.NeuronChargeDecay + weightedInput,
                            AppProperties.NeuronMinCharge,
                            AppProperties.NeuronMaxCharge);

                        if (neuron.Charge > AppProperties.NeuronChargeThreshold)
                        {
                            neuron.State = NeuronState.Excited0;
                            outputVector[j] = 1.0f;
                        }
                        else
                        {
                            outputVector[j] = 0.0f;
                        }
                        break;

                    case NeuronState.Excited0:
                        neuron.State = NeuronState.Excited1;
                        outputVector[j] = 1.0f;
                        break;

                    case NeuronState.Excited1:
                        neuron.State = NeuronState.Recovering0;
                        outputVector[j] = 0.0f;
                        break;

                    case NeuronState.Recovering0:
                        neuron.State = NeuronState.Recovering1;
                        outputVector[j] = 0.0f;
                        break;

                    case NeuronState.Recovering1:
                        neuron.State = NeuronState.Idle;
                        neuron.Charge = 0.0f;
                        outputVector[j] = 0.0f;
                        break;
                }
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

        public void CleanOutputs()
        {
            for (int i = 0; i < VectorSize; ++i)
            {
                InputVector[i] = 0.0f;
                OutputVector[i] = 0.0f;
            }
        }

        private void CloneRegular(NeuronNetwork other, Random rnd)
        {
            Neurons = other.Neurons
                .Select(x => Neuron.CloneFrom(x, rnd))
                .ToArray();

            InputVector = other.InputVector.Select(x => x).ToArray();
            OutputVector = other.OutputVector.Select(x => x).ToArray();
        }

        private void CloneSevereRandomValues(NeuronNetwork other, Random rnd, float severity)
        {
            Neurons = other.Neurons
                .Select(x =>
                    (rnd.NextDouble() < severity) 
                        ? Neuron.CloneFromWithSevereRandom(x, rnd) 
                        : Neuron.CloneFrom(x, rnd))
                .ToArray();

            InputVector = other.InputVector.Select(x => x).ToArray();
            OutputVector = other.OutputVector.Select(x => x).ToArray();
        }


        public void CloneFrom(NeuronNetwork other, Random rnd, bool severeMutations, float severity)
        {
            if (!severeMutations)
            {
                CloneRegular(other, rnd);
            }
            else
            {
                CloneSevereRandomValues(other, rnd, severity);
            }
        }
    }
}
