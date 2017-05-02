using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Neurolution
{
    [Serializable]
    public class Cell
    {
        public NeuronNetwork Network;

        [NonSerialized]
        public const double TailLength = AppProperties.CellTailLength;

        [NonSerialized]
        public const double EyeBase = AppProperties.CellEyeBase;

        public double LocationX = 0.0;
        public double LocationY = 0.0;
        public double Rotation = 0.0;

        [NonSerialized]
        public double PredatorSmellValue = 0.0;

        [NonSerialized]
        public double MoveForceLeft = 0.0;

        [NonSerialized]
        public double MoveForceRight = 0.0;

        //public bool Alive = true;

        [NonSerialized]
        public int ClonedFrom = -1;

        [NonSerialized]
        public double CurrentEnergy = 0.0;

        [NonSerialized]
        public Random Random;

        public Neuron[] Eye => Network.Eye;

        public Cell()
        {
            Random = new Random();
        }

        public Cell(Random r, int maxX, int maxY)
        {
            LocationX = r.Next(maxX);
            LocationY = r.Next(maxY);
            Rotation = r.NextDouble() * 2.0 * Math.PI;
            Network = new NeuronNetwork(AppProperties.InitialNetworkSize, r);

            Random = new Random(r.Next());
        }

        public void PrepareIteration()
        {
            Network.PrepareIteration();
        }

        // set sensors 
        // call Iterate
        // digest Motor* params 
        public void IterateNetwork(long step)
        {
            Network.InputVector[AppProperties.PredatorSmellSensor] = PredatorSmellValue;

            Network.IterateNetwork(Random);

            MoveForceLeft = Network.OutputVector[AppProperties.NetworkMoveForceLeft];
            MoveForceRight = Network.OutputVector[AppProperties.NetworkMoveForceRight];
        }

        public void CloneFrom(Cell other, Random rnd, int maxX, int maxY, bool severeMutations, double severity)
        {
            RandomizeLocation(rnd, maxX, maxY);

            //Alive = true;

            Network.CloneFrom(other.Network, rnd, severeMutations, severity);
        }

        public void RandomizeLocation(Random rnd, int maxX, int maxY)
        {
            LocationX = rnd.Next(maxX);
            LocationY = rnd.Next(maxY);
            Rotation = rnd.NextDouble() * 2.0 * Math.PI;        
        }
    }

    public sealed class CellUtils
    {
        public static Cell ReadCell(string filename)
        {
            Cell ret = null;

            XmlSerializer serializer = new XmlSerializer(typeof(Cell));

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                ret = (Cell) serializer.Deserialize(fs);
            }

            return ret;
        }

        public static List<Cell> ReadCells(string filename)
        {
            List<Cell> ret = null;

            XmlSerializer serializer = new XmlSerializer(typeof(List<Cell>));

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                ret = (List<Cell>)serializer.Deserialize(fs);
            }

            return ret;
        }

        public static void SaveCell(string filename, Cell cell)
        {
            XmlSerializer serializer =new XmlSerializer(typeof(Cell));
            using (TextWriter writer = new StreamWriter(filename))
            {
                serializer.Serialize(writer, cell);
            } 
        }

        public static void SaveCells(string filename, List<Cell> cells)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Cell>));
            using (TextWriter writer = new StreamWriter(filename))
            {
                serializer.Serialize(writer, cells);
            }
        }
    }
}
