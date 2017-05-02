using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Neurolution
{
    public class Predator
    {
        public double Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public double LocationX { get; set; }
        public double LocationY { get; set; }
        private double _value;

        public Predator(Random rnd, int maxX, int maxY)
        {
            Reset(rnd, maxX, maxY);
        }

        public void Reset(Random rnd, int maxX, int maxY)
        {
            Value = AppProperties.PredatorInitialValue;// * (0.5 + rnd.NextDouble());
            double radius = AppProperties.FoodMinDistanceToBorder;

            LocationX = radius + rnd.Next(maxX - 2 * (int)radius);
            LocationY = radius + rnd.Next(maxY - 2 * (int)radius);
        }

        public void Eat(double addValue)
        {
            for(;;)
            {
                double valueCopy = Value;
                double newValue = valueCopy + addValue;

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
        public double Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public double LocationX { get; set; }
        public double LocationY { get; set; }
        private double _value;

        public Food(Random rnd, int maxX, int maxY)
        {
            Reset(rnd, maxX, maxY);
        }

        public double Consume(double delta = 0.071)
        {
            double ret = 0.0;

            while (Value > 0.001)
            {
                double valueCopy = Value;
                double newDelta = valueCopy > AppProperties.InitialCellEnergy * 0.9 ? 0.1 : 0.01;
                double newValue = valueCopy * (1 - newDelta);

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
            double radius = AppProperties.FoodMinDistanceToBorder;

            LocationX = radius + rnd.Next(maxX - 2 * (int)radius);
            LocationY = radius + rnd.Next(maxY - 2 * (int)radius);
        }
    }

    public class World
    {
        public const double Sqrt2 = 1.4142135623730950488016887242097;

        public const double FoodRadiusSquare = AppProperties.FoodRadius * AppProperties.FoodRadius;

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
            Cells = new Cell[size];

            for (int i = 0; i < size; ++i)
                Cells[i] = new Cell(_random, maxX, maxY);

            Foods = new Food[foodItems];
            for (int i = 0; i < foodItems; ++i)
                Foods[i] = new Food(_random, maxX, maxY);

            Predators = new Predator[predatorItems];
            for (int i = 0; i < predatorItems; ++i)
                Predators[i] = new Predator(_random, maxX, maxY);

            _maxX = maxX;
            _maxY = maxY;
            _workingFolder = workingFolder;
        }

        public void InitializeFromTopFile(string filename)
        {
            Cell masterCell = CellUtils.ReadCell(filename);
            if (masterCell != null)
            {
                foreach (var cell in Cells)
                {
                    cell.CloneFrom(masterCell, _random, _maxX, _maxY, false, 0.0);
                    cell.Network.UpdateEye();
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
                    cell.Network.UpdateEye();
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
            string filename = $"{_workingFolder}\\{step:D8}-{now:yyyy-MM-dd-HH-mm-ss}-top.xml";

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
            string filename = $"{_workingFolder}\\{step:D8}-{now:yyyy-MM-dd-HH-mm-ss}-world.xml";

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
                FoodReset();

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

                            double energy = AppProperties.InitialCellEnergy;
                            src.CurrentEnergy -= AppProperties.BirthEnergyConsumption;

                            MakeBaby(
                                source: src,
                                destination: dst,
                                initialEnergy: energy
                            );
                        }
                    }

                }

            }
        }

        public void IterateCell(long step, Cell cell)
        {
            double bodyDirectionX = Math.Cos(cell.Rotation);
            double bodyDirectionY = Math.Sin(cell.Rotation);

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
                        Distnace = Math.Sqrt(
                                Math.Pow(item.LocationX - cell.LocationX, 2) +
                                Math.Pow(item.LocationY - cell.LocationY, 2) 
                                )
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
                        Distnace = Math.Sqrt(
                                Math.Pow(item.LocationX - cell.LocationX, 2) +
                                Math.Pow(item.LocationY - cell.LocationY, 2)
                                )
                    })
                .ToArray();

            foreach (var eyeCell in cell.Eye)
            {
                // 
                double viewDirection = cell.Rotation + eyeCell.LightSensitivity.Direction;

                double viewDirectionX = Math.Cos(viewDirection);
                double viewDirectionY = Math.Sin(viewDirection);

                double value = 0.0;


                foreach (var food in foodDirectoins)
                {
                    double modulo = viewDirectionX * food.DirectionX + viewDirectionY * food.DirectionY;

                    if (modulo <= 0.0)
                        continue;

                    double cosine = modulo / food.Distnace;

                    double distnaceSquare = Math.Pow(food.Distnace, 2);

                    double signalLevel = 
                        food.Item.Value * Math.Pow(cosine, eyeCell.LightSensitivity.Width) 
                            / distnaceSquare;

                    value += signalLevel;
                }

                foreach (var predator in predatorDirections)
                {
                    double modulo = viewDirectionX * predator.DirectionX + viewDirectionY * predator.DirectionY;

                    if (modulo <= 0.0)
                        continue;

                    double cosine = modulo / predator.Distnace;

                    double distnaceSquare = Math.Pow(predator.Distnace, 2);

                    double signalLevel =
                        predator.Item.Value * Math.Pow(cosine, eyeCell.LightSensitivity.Width)
                            / distnaceSquare;

                    value += signalLevel;
                }

                cell.Network.InputVector[eyeCell.LightSensitivity.InputIdx] = value;
            }

            double predatorSmell = 0.0;
            foreach (var predator in Predators)
            {
                double distanceSquare =
                    Math.Pow(cell.LocationX - predator.LocationX, 2) +
                    Math.Pow(cell.LocationY - predator.LocationY, 2);

                predatorSmell += 10000.0 / distanceSquare;
            }
            cell.PredatorSmellValue = predatorSmell;

            double repellingSignalDX = 0.0;
            double repellingSignalDY = 0.0;
            double attractingSignalDX = 0.0;
            double attractingSignalDY = 0.0;

            foreach (var food in foodDirectoins)
            {
                double distnaceSquareSquare = Math.Pow(food.Distnace, 2);

                if (distnaceSquareSquare < FoodRadiusSquare)
                    continue;

                repellingSignalDX += food.Item.Value * food.DirectionX / distnaceSquareSquare;
                repellingSignalDY += food.Item.Value * food.DirectionY / distnaceSquareSquare;
            }

            foreach (var predator in predatorDirections)
            {
                double distnaceSquareSquare = Math.Pow(predator.Distnace, 2);

                if (distnaceSquareSquare < FoodRadiusSquare)
                    continue;

                attractingSignalDX += predator.Item.Value * predator.DirectionX / distnaceSquareSquare;
                attractingSignalDY += predator.Item.Value * predator.DirectionY / distnaceSquareSquare;
            }

            // OK, let cell think
            double brainNetworkRequired = cell.Network.EnergeyPerIteration;

            if (brainNetworkRequired <= cell.CurrentEnergy)
            {
                cell.IterateNetwork(step);
                cell.CurrentEnergy -= brainNetworkRequired;
            }
            else
            {
                cell.CurrentEnergy = 0.0; 
            }

            // Execute action - what is ordered by the neuron network
            double forceLeft = cell.MoveForceLeft;
            double forceRight = cell.MoveForceRight;

//            double forwardForce = Math.Max(0.0, (forceLeft + forceRight) / Sqrt2 * 2.0); // can only move forward
            double forwardForce = ((forceLeft + forceRight) / Sqrt2 * 2.0); 
            double rotationForce = (forceLeft - forceRight) / Sqrt2;

            double moveEnergyRequired = 
                (Math.Abs(forceLeft) + Math.Abs(forceRight)) * AppProperties.MoveEnergyFactor;

            if (moveEnergyRequired <= cell.CurrentEnergy)
            {
                cell.CurrentEnergy -= moveEnergyRequired;

                cell.Rotation += rotationForce;
                if (cell.Rotation > Math.PI*2.0)
                    cell.Rotation -= Math.PI*2.0;
                else if (cell.Rotation < 0.0)
                    cell.Rotation += Math.PI*2.0;

                double dX = forwardForce*Math.Cos(cell.Rotation);
                double dY = forwardForce*Math.Sin(cell.Rotation);

                cell.LocationX += dX;
                cell.LocationY += dY;
            }
            else
            {
                cell.CurrentEnergy = 0.0; // so it has tried and failed
            }

            cell.LocationX += 
                - AppProperties.FoodRepellingForce * repellingSignalDX 
                    + AppProperties.PredatorAttracktion * attractingSignalDX;

            cell.LocationY += 
                - AppProperties.FoodRepellingForce * repellingSignalDY
                    + AppProperties.PredatorAttracktion * attractingSignalDY;

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

                    double distanceSquare =
                        Math.Pow(cell.LocationX - food.LocationX, 2.0) +
                        Math.Pow(cell.LocationY - food.LocationY, 2.0);

                    if (distanceSquare < FoodRadiusSquare)
                    {
                        cell.CurrentEnergy += food.Consume();
                    }
                }
            }

            foreach (var predator in Predators)
            {
                double distanceSquare =
                    Math.Pow(cell.LocationX - predator.LocationX, 2.0) +
                    Math.Pow(cell.LocationY - predator.LocationY, 2.0);

                if (distanceSquare < FoodRadiusSquare)
                {
                    predator.Eat(cell.CurrentEnergy);
                    cell.CurrentEnergy = 0.0; // GONE!!
                }
            }
        }

        private void FoodReset()
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

            FoodReset();
        }

        public void MakeBaby(Cell source, Cell destination, double initialEnergy)
        {
            var rv = _random.NextDouble();
            bool severeMutations = (rv < AppProperties.SevereMutationFactor);

            double severity = 1.0 - Math.Pow(rv / AppProperties.SevereMutationFactor,
                AppProperties.SevereMutationSlope); // % of neurons to mutate

            destination.CloneFrom(source, _random, _maxX, _maxY, severeMutations, severity);
            destination.ClonedFrom = -1;

            destination.CurrentEnergy = initialEnergy;
            destination.Network.CleanOutputs();

            destination.RandomizeLocation(_random, _maxX, _maxY);
        }
    }
}
