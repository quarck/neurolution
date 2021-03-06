﻿using System;
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
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        private float _value;

        public Predator(Random rnd, int maxX, int maxY)
        {
            if (rnd != null)
                Reset(rnd, maxX, maxY);
        }

        public void Reset(Random rnd, int maxX, int maxY, bool valueOnly = false)
        {
            Value = AppProperties.PredatorInitialValue;// * (0.5 + rnd.NextDouble());

            if (!valueOnly)
            {
                LocationX = rnd.Next(maxX);
                LocationY = rnd.Next(maxY);
                DirectionX = (float)(rnd.NextDouble() - 0.5);
                DirectionY = (float)(rnd.NextDouble() - 0.5);
            }
        }

        public void Eat(float addValue)
        {
            for (; ; )
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

        public void Step(Random rnd, int maxX, int maxY)
        {
            LocationX = (LocationX + DirectionX + (float)(rnd.NextDouble() * 0.25 - 0.125) + maxX) % maxX;
            LocationY = (LocationY + DirectionY + (float)(rnd.NextDouble() * 0.25 - 0.125) + maxY) % maxY;
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
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
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
                float newDelta = Math.Min(valueCopy, 0.5f);
                float newValue = valueCopy - newDelta;

                if (Interlocked.CompareExchange(ref _value, newValue, valueCopy) == valueCopy)
                {
                    ret = valueCopy - newValue;
                    break;
                }
            }

            return ret;
        }

        public bool IsEmpty => Value < 0.00001;

        public void Reset(Random rnd, int maxX, int maxY, bool valueOnly = false)
        {
            Value = AppProperties.FoodInitialValue;// * (0.5 + rnd.NextDouble());

            if (!valueOnly)
            {
                LocationX = rnd.Next(maxX);
                LocationY = rnd.Next(maxY);

                DirectionX = (float)(rnd.NextDouble() * 0.5 - 0.25);
                DirectionY = (float)(rnd.NextDouble() * 0.5 - 0.25);
            }
        }

        public void Step(Random rnd, int maxX, int maxY)
        {
            LocationX = (LocationX + DirectionX + (float)(rnd.NextDouble() * 0.25 - 0.125) + maxX) % maxX;
            LocationY = (LocationY + DirectionY + (float)(rnd.NextDouble() * 0.25 - 0.125) + maxY) % maxY;
        }
    }

    public struct FoodDirection
    {
        public Food Item;
        public float DirectionX;
        public float DirectionY;
        public float DistanceSquare;

        public FoodDirection(Food item, float dx, float dy)
        {
            Item = item;
            DirectionX = dx;
            DirectionY = dy;
            DistanceSquare = dx * dx + dy * dy;
        }

        public void Set(Food item, float dx, float dy)
        {
            Item = item;
            DirectionX = dx;
            DirectionY = dy;
            DistanceSquare = dx * dx + dy * dy;
        }
    }

    public struct PredatorDirection
    {
        public Predator Item;
        public float DirectionX;
        public float DirectionY;
        public float DistanceSquare;

        public PredatorDirection(Predator item, float dx, float dy)
        {
            Item = item;
            DirectionX = dx;
            DirectionY = dy;
            DistanceSquare = dx * dx + dy * dy;
        }

        public void Set(Predator item, float dx, float dy)
        {
            Item = item;
            DirectionX = dx;
            DirectionY = dy;
            DistanceSquare = dx * dx + dy * dy;
        }
    }

    public class World
    {
        public const float Sqrt2 = 1.4142135623730950488016887242097f;

        public Cell[] Cells;

        public Food[] Foods;
        public Predator[] Predators;

        [ThreadStatic]
        private FoodDirection[] foodDirections;

        [ThreadStatic]
        private PredatorDirection[] predatorDirections;

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
            for (int i = 0; i < predatorItems; ++i)
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

        public void SaveBest(long step)
        {
            var best =
                Cells
                    .OrderByDescending(cell => cell.CurrentEnergy)
                    .First();

            SerializeBest(best, step);
        }


        public void Iterate(long step)
        {
            if (step == 0)
                WorldInitialize();

            // restore any foods
            foreach (var food in Foods)
                food.Step(_random, _maxX, _maxY);

            foreach (var predator in Predators)
                predator.Step(_random, _maxX, _maxY);

            if ((step % AppProperties.StepsPerGeneration) == 0)
            {
                foreach (var predator in Predators)
                    predator.Reset(_random, _maxX, _maxY, true);

                foreach (var food in Foods)
                    food.Reset(_random, _maxX, _maxY);
            }


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

                    //                    if (step % AppProperties.SerializeWorldEveryNStep == 0)
                    //                        SerializeWorld(sortedWorld.ToList(), step);
                }

                int srcIdx = 0;
                int dstIdx = sortedWorld.Length - 1; //quant * 4;

                foreach (var multiplier in new[] { 6, 3, 2, 1 })
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


        // Quick reverse square root from Quake 3 source code 
        private static unsafe float Q_rsqrt(float number)
        {
            int i;
            float x2, y;
            const float threehalfs = 1.5F;

            x2 = number * 0.5F;
            y = number;
            i = *(int*)&y;                           // evil floating point bit level hacking
            i = 0x5f3759df - (i >> 1);               // what the fuck? 
            y = *(float*)&i;
            y = y * (threehalfs - (x2 * y * y));   // 1st iteration

            return y;
        }


        public void IterateCell(long step, Cell cell)
        {
            if (foodDirections == null)
            {
                foodDirections = new FoodDirection[Foods.Length];
                for (int idx = 0; idx < Foods.Length; ++idx)
                    foodDirections[idx] = new FoodDirection(null, 0.0f, 0.0f);
            }

            if (predatorDirections == null)
            {
                predatorDirections = new PredatorDirection[Predators.Length];
                for (int idx = 0; idx < Predators.Length; ++idx)
                    predatorDirections[idx] = new PredatorDirection(null, 0.0f, 0.0f);
            }


            cell.PrepareIteration();

            float offsX = _maxX * 1.5f - cell.LocationX;
            float offsY = _maxY * 1.5f - cell.LocationY;
            float halfMaxX = _maxX / 2.0f;
            float halfMaxY = _maxY / 2.0f;

            // Calculate light sensor values 
            for (int idx = 0; idx < Foods.Length; ++idx)
            {
                var item = Foods[idx];

                float dx = (item.LocationX + offsX) % _maxX - halfMaxX;
                float dy = (item.LocationY + offsY) % _maxY - halfMaxY;

                foodDirections[idx].Set(item, dx, dy);
            }

            for (int idx = 0; idx < Predators.Length; ++idx)
            {
                var item = Predators[idx];

                float dx = (item.LocationX + offsX) % _maxX - halfMaxX;
                float dy = (item.LocationY + offsY) % _maxY - halfMaxY;

                predatorDirections[idx].Set(item, dx, dy);
            }

            for (int eyeIdx = 0; eyeIdx < cell.Eye.Length; ++eyeIdx)
            {
                var eyeCell = cell.Eye[eyeIdx];
                // 
                float viewDirection = cell.Rotation + eyeCell.Direction;

                float viewDirectionX = (float)Math.Cos(viewDirection);
                float viewDirectionY = (float)Math.Sin(viewDirection);

                float value = 0.0f;


                if (eyeCell.SensetiveToRed)
                {
                    // This cell can see foods only
                    foreach (var food in foodDirections)
                    {
                        float modulo = viewDirectionX * food.DirectionX + viewDirectionY * food.DirectionY;

                        if (modulo <= 0.0)
                            continue;

                        float invSqrRoot = Q_rsqrt(food.DistanceSquare);

                        float cosine = modulo * invSqrRoot;

                        //float distnaceSquare = (float) Math.Pow(food.Distnace, 2);

                        float signalLevel =
                            (float)(food.Item.Value * Math.Pow(cosine, eyeCell.Width)
                                * invSqrRoot * invSqrRoot);

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

                        float invSqrRoot = Q_rsqrt(predator.DistanceSquare);

                        float cosine = modulo * invSqrRoot;

                        //                        float distnaceSquare = (float)Math.Pow(predator.Distnace, 2);

                        float signalLevel =
                            (float)(predator.Item.Value * Math.Pow(cosine, eyeCell.Width)
                                * invSqrRoot * invSqrRoot);

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
                if (cell.Rotation > Math.PI * 2.0)
                    cell.Rotation -= (float)(Math.PI * 2.0);
                else if (cell.Rotation < 0.0)
                    cell.Rotation += (float)(Math.PI * 2.0);

                float dX = (float)(forwardForce * Math.Cos(cell.Rotation));
                float dY = (float)(forwardForce * Math.Sin(cell.Rotation));

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

                    float dv = (float)(Math.Sqrt(food.Value) / 2.0 * 5);

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

        private void WorldInitialize()
        {
            // cleanput outputs & foods 
            foreach (var cell in Cells)
            {
                cell.CurrentEnergy = AppProperties.InitialCellEnergy;
                cell.Network.CleanOutputs();
                //cell.RandomizeLocation(_random, _maxX, _maxY);
            }

            foreach (var food in Foods)
                food.Reset(_random, _maxX, _maxY);

            foreach (var predator in Predators)
                predator.Reset(_random, _maxX, _maxY);
        }

        public void MakeBaby(Cell source, Cell destination, float initialEnergy)
        {
            var rv = _random.NextDouble();
            bool severeMutations = (rv < AppProperties.SevereMutationFactor);

            float severity = (float)(1.0 - Math.Pow(rv / AppProperties.SevereMutationFactor,
                                          AppProperties.SevereMutationSlope)); // % of neurons to mutate

            destination.CloneFrom(source, _random, _maxX, _maxY, severeMutations, severity);
            destination.ClonedFrom = -1;

            destination.CurrentEnergy = initialEnergy;
            destination.Network.CleanOutputs();

            destination.RandomizeLocation(_random, _maxX, _maxY);
        }
    }
}
