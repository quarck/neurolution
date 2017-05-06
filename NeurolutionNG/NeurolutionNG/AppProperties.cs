using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neurolution
{
    public sealed class AppProperties
    {
        public const string SerializeTo = @"c:\users\spars\Desktop\cell.xml";

        public const int RedEyeSize = 24;
        public const int BlueEyeSize = 24;

        public const int EyeSize = RedEyeSize + BlueEyeSize;

        public const float EyeAngle = (float)Math.PI * 2.0f; // a little bit of insect eye (looking backwards)
        public const float EyeCellDirectionStep = (float) ( EyeAngle / EyeSize);

        public const float EyeCellWidth = 0.1f;


        public const int StepsPerGeneration = 512;
        public const int StepsPerBirthCheck = 512;

        public const int SerializeTopEveryNStep = 8192 * 8;
        public const int SerializeWorldEveryNStep = 8192 * 64;

        public const int NetworkSize = 256;

        public const int WorldSize = 128;
        public const int FoodCountPerIteration = 32;
        public const int PredatorCountPerIteration = 4;


        public const int NetworkMoveForceLeft = RedEyeSize + BlueEyeSize + 0;
        public const int NetworkMoveForceRight = RedEyeSize + BlueEyeSize + 1;
        public const int NetworkLastSpecialIdx = NetworkMoveForceRight;

        public const float NetworkMaxRegularMutation = 0.03f;
        public const float NetworkSevereMutationAlpha = 0.4f;
        public const float NetworkNoiseLevel = 0.00001f;

        public const float FoodInitialValue = 10;
//        public const float FoodRadius = 5;

        public const float PredatorInitialValue = 10;
  //      public const float PredatorRadius = 15;

        public const float CellTailLength = 4.0f;
        public const float CellEyeBase = 3.0f;

        public const int WorldWidth = 800;
        public const int WorldHeight = 600;

        public const float SevereMutationFactor = 0.15f;
        public const float SevereMutationSlope = 0.33f;

        public const float FoodMinDistanceToBorder = 100;

        public const float MoveEnergyFactor = 0.0000001f;
        public const float InitialCellEnergy = 1.0f;

        public const float MaxEnergyCapacity = 14.0f;

        public const float BirthEnergyConsumption = 2.0f;

        public const float SporeEnergyLevel = 0.01f;

        public const float NeuronChargeDecay = 0.30f;

        public const float NeuronMinCharge = -2.0f;
        public const float NeuronMaxCharge = 2.0f;

        public const float NeuronChargeThreshold = 1.0f;

        public const long OldSince = 1024;
    }
}
