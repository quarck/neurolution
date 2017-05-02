using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Neurolution
{
    class WorldView
    {
        private World _world;

        private Gdk.Window _window;

        public CellView[] CellViews;

        public Rectangle[] FoodLocations;

        public Rectangle[] PredatorLocations;

        public WorldView(Gtk.Widget canvas, World world)
        {
            _world = world;
            _window = canvas.GdkWindow;

            Random rnd = new Random();

            var brush = new Gdk.GC(_window);
            brush.Copy(canvas.Style.ForegroundGC(Gtk.StateType.Normal));
            brush.RgbFgColor = new Gdk.Color(192, 64, 200);

            var predatorBrush = new Gdk.GC(_window);
            predatorBrush.Copy(canvas.Style.ForegroundGC(Gtk.StateType.Normal));
            predatorBrush.RgbFgColor = new Gdk.Color(64, 64, 255);

            FoodLocations = new Rectangle[_world.Foods.Length];

            for (int i = 0; i < FoodLocations.Length; ++i)
            {
                FoodLocations[i] =
                    new Rectangle
                    {
                        Stroke = brush,
                        Filled = true,
                        X = 0,
                        Y = 0,
                        Width = 0,
                        Height = 0
                    };
            }

            PredatorLocations = new Rectangle[_world.Predators.Length];

            for (int i = 0; i < PredatorLocations.Length; ++i)
            {
                PredatorLocations[i] =
                    new Rectangle
                    {
                        Stroke = predatorBrush,
                        Filled = true,
                        X = 0,
                        Y = 0,
                        Width = 0,
                        Height = 0
                    };
            }

            CellViews = new CellView[_world.Cells.Length];

            for (int i = 0; i < _world.Cells.Length; ++i)
            {
                CellViews[i] = new CellView(canvas, _world.Cells[i], rnd);
            }
        }

        public void Draw()
        {
            foreach (var cellView in CellViews)
            {
                cellView.Draw();
            }

            for (int i = 0; i < FoodLocations.Length; ++i)
            {
                FoodLocations[i].Draw(_window);
            }

            for (int i = 0; i < PredatorLocations.Length; ++i)
            {
                PredatorLocations[i].Draw(_window);
            }
        }


        public void UpdateFrom(World world)
        {
            foreach (var cellView in CellViews)
            {
                cellView.Update();
            }

            for (int i = 0; i < FoodLocations.Length; ++i)
            {
                var food = world.Foods[i];

                var diameter = Math.Sqrt(food.Value) * 5;

                FoodLocations[i].X = (int)(food.LocationX - diameter/2.0);
                FoodLocations[i].Y = (int)(food.LocationY - diameter/2.0);
                FoodLocations[i].Width = (int)diameter;
                FoodLocations[i].Height = (int)diameter;
            }

            for (int i = 0; i < PredatorLocations.Length; ++i)
            {
                var predator = world.Predators[i];

                var diameter = Math.Sqrt(predator.Value) * 5;

                PredatorLocations[i].X = (int)(predator.LocationX - diameter/2.0);
                PredatorLocations[i].Y = (int)(predator.LocationY - diameter/2.0);
                PredatorLocations[i].Width = (int)diameter;
                PredatorLocations[i].Height = (int)diameter;
            }
        }
    }
}
