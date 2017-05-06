// * https://github.com/mono/gtk-sharp/blob/master/sample/Scribble.cs
// * http://lists.dot.net/pipermail/mono-list/2007-February/034352.html
// * Fix .Consume
// * Foods and Predators are just points -- take it's size (angular) into account
// * Dont' calculate charge difference for excited/recovering neurons
// * Next iteration if total leftover of foods is less than 30% and no consumption in the last 16 steps
// * Dynamic new foods additions and births, no fixed limit and fixed iteration cycles 
// * Display cell energy via the color (Green - 100%, Red - 0%)
// * Moving foods and predators (Predators -- don't even change, just run randomly all the time)
// * Predators should have gravity
// * Predators should have neuron network, ability to control gravity and move, plus they should have some simple sensor
// * Cells should emit a number of signals that could be heard by nearby, and nearby should be able to receive it


using System;
using Gtk;
using Gdk;
using System.Threading;
using Neurolution;


public partial class MainWindow: Gtk.Window
{
    private readonly World _world;
    private WorldView _worldView;

    private Thread _thread = null;
    private CancellationTokenSource _cancelTokenSource = null;

    private volatile bool isDrawing = false;

    private Gdk.Rectangle _viewPort = 
        new Gdk.Rectangle(0, 0, 
            AppProperties.WorldWidth,
            AppProperties.WorldHeight);

    private long _lastStep = 0;

    public MainWindow () : base (Gtk.WindowType.Toplevel)
    {
        Build ();
        canvas.ExposeEvent += OnExposed;

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string workingFolder = $"{documents}/Neurolution/{DateTime.Now:yyyy-MM-dd-HH-mm}";

        _world = new World(
            workingFolder,
            AppProperties.WorldSize,
            AppProperties.FoodCountPerIteration,
            AppProperties.PredatorCountPerIteration,
            AppProperties.WorldWidth,
            AppProperties.WorldHeight);

        #if DEBUG
        _world.MultiThreaded = false;
        #else
        _world.MultiThreaded = true;
        #endif
    }

    protected void OnDeleteEvent (object sender, DeleteEventArgs a)
    {
        if (_cancelTokenSource != null)
        {
            _cancelTokenSource.Cancel();

            for (int i = 0; i < 20; ++i)
            {
                if (_thread == null)
                    break;
                Thread.Sleep(300);
            }             
        }

        if (_thread != null)
            _thread.Abort();

        Gtk.Application.Quit ();
        a.RetVal = true;
    }

    void OnExposed(object o, ExposeEventArgs args) 
    {
        isDrawing = true;

        try 
        {
            if (_worldView != null)
            {
                canvas.GdkWindow.DrawRectangle(canvas.Style.BackgroundGC(StateType.Normal), true, _viewPort);
                _worldView.Draw();
            }

//            statusLabel.Text = $"Step {_lastStep}";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ex: {ex}, trace: {ex.StackTrace}");
            Environment.Exit(-1);
        }

        isDrawing = false;
    }

    private void CalcThread()
    {
        var token = _cancelTokenSource.Token;

        //var end = DateTime.Now + TimeSpan.FromSeconds(20);

        for (long step = 0; ; ++step)
        {
            lock (_world)
            {
                _world.Iterate(step);
                _lastStep = step;
            }

            if (step % 4 == 0)
            {
                UpdateUI(step);
                if (token.IsCancellationRequested)
                    break;
            }
        }

        //statusLabel.Text = "Saving world";
        _world.Save();

        _thread = null;
    }

    private void UpdateUI(long step)
    {
        lock (_world)
        {
            _worldView.UpdateFrom(_world);
        }

        statusLabel.Text = $"Step {_lastStep}";

        if (!isDrawing)
            canvas.QueueDrawArea(0, 0, AppProperties.WorldWidth, AppProperties.WorldHeight);
    }

    protected void HideControls()
    {
        runbutton.Hide ();
        loadButton.Hide ();
        loadSavedWorld.Hide ();
    }

    protected void RunWorld()
    {
        HideControls ();

        canvas.SetSizeRequest (AppProperties.WorldWidth, AppProperties.WorldHeight);

        _worldView = new WorldView(canvas, _world);

        _cancelTokenSource = new CancellationTokenSource();
        _thread = new Thread (this.CalcThread);
        _thread.Start ();
    }

    protected void OnRunbuttonClicked (object sender, EventArgs e)
    {
        RunWorld();
    }

    protected void OnLoadTopButtonClicked (object sender, EventArgs e)
    {
        Gtk.FileChooserDialog filechooser =
            new Gtk.FileChooserDialog("Choose the file to open",
                this,
                FileChooserAction.Open,
                "Cancel",ResponseType.Cancel,
                "Open",ResponseType.Accept);

        filechooser.Filter = new FileFilter();
        filechooser.Filter.AddPattern("*.xml");

        if (filechooser.Run() == (int)ResponseType.Accept) 
        {
            _world.InitializeFromTopFile(filechooser.Filename);
            RunWorld();
        }

        filechooser.Destroy();
    }

    protected void OnLoadSavedWorldClicked (object sender, EventArgs e)
    {
        Gtk.FileChooserDialog filechooser =
            new Gtk.FileChooserDialog("Choose the file to open",
                this,
                FileChooserAction.Open,
                "Cancel",ResponseType.Cancel,
                "Open",ResponseType.Accept);

        filechooser.Filter = new FileFilter();
        filechooser.Filter.AddPattern("*.xml");

        if (filechooser.Run() == (int)ResponseType.Accept) 
        {
            _world.InitializeFromWorldFile(filechooser.Filename);
            RunWorld();
        }

        filechooser.Destroy();
    }
}
