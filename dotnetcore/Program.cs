using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neurolution;

namespace neurolution
{
    class Program
    {
        static void Main(string[] args)
        {
            bool multiThreaded = true; //args.Any(x => x == "-mt" || x == "--mt");

            string workingFolder = "{documents}/Neurolution/{DateTime.Now:yyyy-MM-dd-HH-mm}";

            var _world = new World(
                workingFolder,
                AppProperties.WorldSize,
                AppProperties.FoodCountPerIteration,
                AppProperties.PredatorCountPerIteration,
                AppProperties.WorldWidth,
                AppProperties.WorldHeight);

            _world.MultiThreaded = multiThreaded;

            var start = DateTime.Now;
            var until = start + TimeSpan.FromSeconds(20);

            for (long step = 0; ; ++step)
            {
                lock (_world)
                {
                    _world.Iterate(step);
                }
                Console.WriteLine($"Step {step}");

                if (DateTime.Now > until)
                    break;
            }

            _world.Save();

        }
    }
}
