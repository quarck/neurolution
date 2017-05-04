using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neurolution
{
    public class Predator
    {
        public float Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public float LocationX { get; set; }
        public float LocationY { get; set; }
        private float _value;

        public Predator(Random rnd, int maxX, int maxY)
        {
            if (rnd != null)
                Reset(rnd, maxX, maxY);
        }

        public void Reset(Random rnd, int maxX, int maxY)
        {
            Value = AppProperties.PredatorInitialValue;// * (0.5 + rnd.NextDouble());
            float radius = AppProperties.FoodMinDistanceToBorder;

            LocationX = radius + rnd.Next(maxX - 2 * (int)radius);
            LocationY = radius + rnd.Next(maxY - 2 * (int)radius);
        }

        public void Eat(float addValue)
        {
            for(;;)
            {
                float valueCopy = Value;
                float newValue = valueCopy + addValue;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Interlocked.CompareExchange(ref _value, newValue, valueCopy) == valueCopy)
                {
                    break;
                }
            }
        }
    }

    public class Food
    {
        public float Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public float LocationX { get; set; }
        public float LocationY { get; set; }
        private float _value;

        public Food(Random rnd, int maxX, int maxY)
        {
            Reset(rnd, maxX, maxY);
        }

        public float Consume(float delta = 0.071f)
        {
            float ret = 0.0f;

            while (Value > 0.001)
            {
                float valueCopy = Value;
                float newDelta = (float) (valueCopy > AppProperties.InitialCellEnergy * 0.9 ? 0.1 : 0.01);
                float newValue = valueCopy * (1 - newDelta);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Interlocked.CompareExchange(ref _value, newValue, valueCopy) == valueCopy)
                {
                    ret = valueCopy - newValue;
                    break;
                }
            }

            return ret;
        }

        public bool IsEmpty => Value < 0.00001;

        public void Reset(Random rnd, int maxX, int maxY)
        {
            Value = AppProperties.FoodInitialValue;// * (0.5 + rnd.NextDouble());
            float radius = AppProperties.FoodMinDistanceToBorder;

            LocationX = radius + rnd.Next(maxX - 2 * (int)radius);
            LocationY = radius + rnd.Next(maxY - 2 * (int)radius);
        }
    }

    public class World
    {
        public const float Sqrt2 = 1.4142135623730950488016887242097f;

//        public const float FoodRadiusSquare = AppProperties.FoodRadius * AppProperties.FoodRadius;
  //      public const float PredatorRadiusSquare = AppProperties.PredatorRadius * AppProperties.PredatorRadius;

        public Cell[] Cells;
        public Food[] Foods;
        public Predator[] Predators;

        private readonly int _maxX;
        private readonly int _maxY;

        private readonly Random _random = new Random();

        private readonly string _workingFolder;
        private bool _workingFolderCreated = false;

        public bool MultiThreaded = false;

        public World(string workingFolder, int size, int foodItems, int predatorItems, int maxX, int maxY)
        {
            _maxX = maxX;
            _maxY = maxY;
            _workingFolder = workingFolder;

            Cells = new Cell[size];

            for (int i = 0; i < size; ++i)
                Cells[i] = new Cell(_random, maxX, maxY);

            Foods = new Food[foodItems];
            for (int i = 0; i < foodItems; ++i)
                Foods[i] = new Food(_random, maxX, maxY);

            Predators = new Predator[predatorItems];
            for(int i = 0; i < predatorItems; ++ i)
                Predators[i] = new Predator(_random, maxX, maxY);
        }

        public void InitializeFromTopFile(string filename)
        {
            Cell masterCell = CellUtils.ReadCell(filename);
            if (masterCell != null)
            {
                foreach (var cell in Cells)
                {
                    cell.CloneFrom(masterCell, _random, _maxX, _maxY, false, 0.0f);
                    cell.Random = new Random(_random.Next());
                    cell.CurrentEnergy = AppProperties.InitialCellEnergy;
                    cell.LocationX = _random.Next(_maxX);
                    cell.LocationY = _random.Next(_maxY);
                }
            }
        }

        public void InitializeFromWorldFile(string filename)
        {
            List<Cell> cells = CellUtils.ReadCells(filename);
            if (cells != null)
            {
                Cells = cells.ToArray();

                foreach (var cell in Cells)
                {
                    cell.Random = new Random(_random.Next());
                    cell.CurrentEnergy = AppProperties.InitialCellEnergy;
                    cell.LocationX = _random.Next(_maxX);
                    cell.LocationY = _random.Next(_maxY);
                }
            }
        }


        private void SerializeBest(Cell cell, long step)
        {
            if (!_workingFolderCreated)
            {
                Directory.CreateDirectory(_workingFolder);
                _workingFolderCreated = true;
            }

            DateTime now = DateTime.Now;
            string filename = $"{_workingFolder}/{step:D8}-{now:yyyy-MM-dd-HH-mm-ss}-top.xml";

            CellUtils.SaveCell(filename, cell);
        }

        public void SerializeWorld(List<Cell> world, long step)
        {
            if (!_workingFolderCreated)
            {
                Directory.CreateDirectory(_workingFolder);
                _workingFolderCreated = true;
            }

            DateTime now = DateTime.Now;
            string filename = $"{_workingFolder}/{step:D8}-{now:yyyy-MM-dd-HH-mm-ss}-world.xml";

            CellUtils.SaveCells(filename, world);
        }

        public void Save()
        {
            SerializeWorld(Cells.ToList(), -1);
        }


        public void Iterate(long step)
        {
            if (step == 0)
                WorldReset();

            if ((step%AppProperties.StepsPerGeneration) == 0)
                FoodAndPredatorReset();

            if (MultiThreaded)
            {
                Parallel.ForEach(
                    Cells,
                    cell => IterateCell(step, cell)
                );
            }
            else
            {
                foreach (var cell in Cells)
                    IterateCell(step, cell);
            }

            if (step != 0 && (step % AppProperties.StepsPerBirthCheck == 0)
                && (Cells.Any(x => x.CurrentEnergy > AppProperties.BirthEnergyConsumption)
                        || (step % AppProperties.SerializeTopEveryNStep == 0)))
            {
                int quant = AppProperties.WorldSize / 16; // == 32 basically

                var sortedWorld =
                    Cells
                        .OrderByDescending(cell => cell.CurrentEnergy)
                        .ToArray();

                if (step % AppProperties.SerializeTopEveryNStep == 0)
                {
                    SerializeBest(sortedWorld[0], step);

                    if (step % AppProperties.SerializeWorldEveryNStep == 0)
                        SerializeWorld(sortedWorld.ToList(), step);
                }

                int srcIdx = 0;
                int dstIdx = sortedWorld.Length - 1; //quant * 4;

                foreach (var multiplier in new[] {6, 3, 2, 1})
                {
                    for (int q = 0; q < quant; ++q)
                    {
                        var src = sortedWorld[srcIdx++];

                        for (int j = 0; j < multiplier; ++j)
                        {
                            if (src.CurrentEnergy < AppProperties.BirthEnergyConsumption)
                                break;

                            var dst = sortedWorld[dstIdx--];

                            float energy = AppProperties.InitialCellEnergy;
                            src.CurrentEnergy -= AppProperties.BirthEnergyConsumption;

                            MakeBaby(
                                source: src,
                                destination: dst,
                                initialEnergy: energy
                            );
                        }
                    }
                }

                foreach (var elderly in Cells.Where(x => x.Age > AppProperties.OldSince))
                {
                    MakeBaby(
                        source: elderly,
                        destination: elderly,
                        initialEnergy: elderly.CurrentEnergy
                    );
                }

            }
        }

        public void IterateCell(long step, Cell cell)
        {
//            float bodyDirectionX = Math.Cos(cell.Rotation);
//            float bodyDirectionY = Math.Sin(cell.Rotation);

            cell.PrepareIteration();

            // Calculate light sensor values 

            var foodDirectoins = Foods
                .Select( 
                    item => 
                    new
                    {
                        Item = item,
                        DirectionX = item.LocationX - cell.LocationX,
                        DirectionY = item.LocationY - cell.LocationY,
                        Distnace = (float)(Math.Sqrt(
                                Math.Pow(item.LocationX - cell.LocationX, 2) +
                                Math.Pow(item.LocationY - cell.LocationY, 2) 
                                ))
                    })
                .ToArray();

            var predatorDirections = Predators
                .Select(
                    item =>
                    new
                    {
                        Item = item,
                        DirectionX = item.LocationX - cell.LocationX,
                        DirectionY = item.LocationY - cell.LocationY,
                        Distnace = (float)(Math.Sqrt(
                                Math.Pow(item.LocationX - cell.LocationX, 2) +
                                Math.Pow(item.LocationY - cell.LocationY, 2)
                                ))
                    })
                .ToArray();

            for (int eyeIdx = 0; eyeIdx < cell.Eye.Length; ++ eyeIdx)
            {
                var eyeCell = cell.Eye[eyeIdx];
                // 
                float viewDirection = cell.Rotation + eyeCell.Direction;

                float viewDirectionX = (float) Math.Cos(viewDirection);
                float viewDirectionY = (float) Math.Sin(viewDirection);

                float value = 0.0f;


                if (eyeCell.SensetiveToRed)
                {
                    // This cell can see foods only
                    foreach (var food in foodDirectoins)
                    {
                        float modulo = viewDirectionX*food.DirectionX + viewDirectionY*food.DirectionY;

                        if (modulo <= 0.0)
                            continue;

                        float cosine = modulo/food.Distnace;

                        float distnaceSquare = (float) Math.Pow(food.Distnace, 2);

                        float signalLevel =
                            (float) (food.Item.Value*Math.Pow(cosine, eyeCell.Width)
                                     /distnaceSquare);

                        value += signalLevel;
                    }
                }
                else
                {
                    // this cell can see predators only
                    foreach (var predator in predatorDirections)
                    {
                        float modulo = viewDirectionX * predator.DirectionX + viewDirectionY * predator.DirectionY;

                        if (modulo <= 0.0)
                            continue;

                        float cosine = modulo / predator.Distnace;

                        float distnaceSquare = (float)Math.Pow(predator.Distnace, 2);

                        float signalLevel =
                            (float)(predator.Item.Value * Math.Pow(cosine, eyeCell.Width)
                                     / distnaceSquare);

                        value += signalLevel;
                    }
                }

                cell.Network.InputVector[eyeIdx] = 1000 * value;
            }


            // Iterate network finally
            cell.IterateNetwork(step);

            // Execute action - what is ordered by the neuron network
            float forceLeft = cell.MoveForceLeft;
            float forceRight = cell.MoveForceRight;

            float forwardForce = ((forceLeft + forceRight) / Sqrt2); 
            float rotationForce = (forceLeft - forceRight) / Sqrt2;

            float moveEnergyRequired = 
                (Math.Abs(forceLeft) + Math.Abs(forceRight)) * AppProperties.MoveEnergyFactor;

            if (moveEnergyRequired <= cell.CurrentEnergy)
            {
                cell.CurrentEnergy -= moveEnergyRequired;

                cell.Rotation += rotationForce;
                if (cell.Rotation > Math.PI*2.0)
                    cell.Rotation -= (float)(Math.PI*2.0);
                else if (cell.Rotation < 0.0)
                    cell.Rotation += (float)(Math.PI*2.0);

                float dX = (float) (forwardForce*Math.Cos(cell.Rotation));
                float dY = (float) (forwardForce*Math.Sin(cell.Rotation));

                cell.LocationX += dX;
                cell.LocationY += dY;
            }
            else
            {
                cell.CurrentEnergy = 0.0f; // so it has tried and failed
            }

            if (cell.LocationX < 0.0)
                cell.LocationX += _maxX;
            else if (cell.LocationX >= _maxX)
                cell.LocationX -= _maxX;

            if (cell.LocationY < 0.0)
                cell.LocationY += _maxY;
            else if (cell.LocationY >= _maxY)
                cell.LocationY -= _maxY;

            if (cell.CurrentEnergy < AppProperties.MaxEnergyCapacity)
            {
                // Analyze the outcome - did it get any food? 
                foreach (var food in Foods)
                {
                    if (food.IsEmpty)
                        continue;

                    float dx = Math.Abs(cell.LocationX - food.LocationX);
                    float dy = Math.Abs(cell.LocationY - food.LocationY);

                    float dv = (float) (Math.Sqrt(food.Value) / 2.0 * 5);

                    if (dx <= dv && dy <= dv)
                    {
                        cell.CurrentEnergy += food.Consume();
                    }
                }
            }

            if (cell.CurrentEnergy > AppProperties.SporeEnergyLevel)
            {
                // Analyze the outcome - did it get any food? 
                foreach (var predator in Predators)
                {
                    float dx = Math.Abs(cell.LocationX - predator.LocationX);
                    float dy = Math.Abs(cell.LocationY - predator.LocationY);

                    float dv = (float)(Math.Sqrt(predator.Value) / 2.0 * 5);

                    if (dx <= dv && dy <= dv)
                    {
                        predator.Eat(cell.CurrentEnergy);
                        cell.CurrentEnergy = 0.0f;
                    }
                }
            }
        }

        private void FoodAndPredatorReset()
        {
            // restore any foods
            foreach (var food in Foods)
                food.Reset(_random, _maxX, _maxY);

            foreach (var predator in Predators)
                predator.Reset(_random, _maxX, _maxY);
        }

        private void WorldReset()
        {
            // cleanput outputs & foods 
            foreach (var cell in Cells)
            {
                cell.CurrentEnergy = AppProperties.InitialCellEnergy;
                cell.Network.CleanOutputs();
                //cell.RandomizeLocation(_random, _maxX, _maxY);
            }

            FoodAndPredatorReset();
        }

        public void MakeBaby(Cell source, Cell destination, float initialEnergy)
        {
            var rv = _random.NextDouble();
            bool severeMutations = (rv < AppProperties.SevereMutationFactor);

            float severity = (float) (1.0 - Math.Pow(rv / AppProperties.SevereMutationFactor,
                                          AppProperties.SevereMutationSlope)); // % of neurons to mutate

            destination.CloneFrom(source, _random, _maxX, _maxY, severeMutations, severity);
            destination.ClonedFrom = -1;

            destination.CurrentEnergy = initialEnergy;
            destination.Network.CleanOutputs();

            destination.RandomizeLocation(_random, _maxX, _maxY);
        }
    }
}
