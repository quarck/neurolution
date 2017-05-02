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

        public const int StepsPerGeneration = 512;
        public const int StepsPerBirthCheck = 64;
        public const int IterationsPerPredatorRelocation = 4;

        public const int SerializeTopEveryNStep = 8192 * 8;
        public const int SerializeWorldEveryNStep = 8192 * 64;

        public const int InitialNetworkSize = 3;

        public const int MinNetworkSize = 3; // absolute minimum - two attachments to legs and that's it
        public const int MaxNetworkSize = 512; // absolute maximum, perormance limitation
        public const double EnergyConsumptionPerNeuron = 0.00001; 

        public const int WorldSize = 512;
        public const int FoodCountPerIteration = 4;
        public const int PredatorCountPerIteration = 0;


        public const int NetworkMoveForceLeft = 0;
        public const int NetworkMoveForceRight = 1;
        public const int PredatorSmellSensor = 2; 
        public const int NetworkLastSpecialIdx = 2;

        public const double NetworkMaxRegularMutation = 0.01;
        public const double NetworkSevereMutationAlpha = 0.3;
        public const double NetworkNoiseLevel = 0.00001;

        public const double FoodInitialValue = 128.0;
        public const double FoodRadius = 5;

        public const double PredatorInitialValue = 40;

        public const double CellTailLength = 4.0;
        public const double CellEyeBase = 3.0;

        public const int WorldWidth = 800;
        public const int WorldHeight = 600;

        public const double SevereMutationFactor = 0.15;
        public const double SevereMutationSlope = 0.33;

        public const double FoodMinDistanceToBorder = 10;

        public const double MoveEnergyFactor = 0.0001;
        public const double InitialCellEnergy = 1.0;

        public const double MaxEnergyCapacity = 14.0;

        public const double LightSensetivityProbability = 0.1;

        public const double BirthEnergyConsumption = 2.0;

        public const double SporeEnergyLevel = 0.01;

        //public const double PredatorAttracktion = 0.26;
        //public const double FoodRepellingForce =  0.06;

        public const double PredatorAttracktion = 0.0;
        public const double FoodRepellingForce = 0.0;
    }
}
