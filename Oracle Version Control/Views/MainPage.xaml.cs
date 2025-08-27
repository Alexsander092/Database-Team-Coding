using Oracle_Version_Control.ViewModels;

namespace Oracle_Version_Control.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isFirstAppearance = true;
    private double _initialColumnWidth;
    private ColumnDefinition? _currentResizingColumn;
    private int _currentColumnIndex = -1;
    private double _startX;
    private const double MIN_COLUMN_WIDTH = 50;
    private const uint ANIMATION_DURATION = 100;
    private const double SCALE_PRESSED = 0.95;
    private const double FADE_PRESSED = 0.9;

    private readonly string[] _resizerNames = { "Col1Resizer", "Col2Resizer", "Col3Resizer", "Col4Resizer", "Col5Resizer" };
    private readonly Button[] _animatedButtons;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _animatedButtons = new Button[]
        {
            CheckoutButton, CheckinButton, LogoutButton, ReloadButton,
            FirstPageButton, PreviousPageButton, NextPageButton, LastPageButton
        };

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, EventArgs e) => SetupColumnResizers();

    private void SetupColumnResizers()
    {
        foreach (var resizerName in _resizerNames)
        {
            if (FindByName(resizerName) is Grid resizer)
            {
                resizer.WidthRequest = 20;
                resizer.BackgroundColor = Colors.Transparent;
                resizer.GestureRecognizers.Clear();

                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += (s, e) => OnColumnResizerPanUpdated(s, e, resizerName);
                resizer.GestureRecognizers.Add(panGesture);
            }
        }
    }

    private void OnColumnResizerPanUpdated(object sender, PanUpdatedEventArgs e, string resizerName)
    {
        int columnIndex = int.Parse(resizerName.AsSpan(3, 1)) - 1;
        var column = TableHeader.ColumnDefinitions[columnIndex];
        var resizer = FindByName(resizerName) as Grid;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                StartColumnResize(e, columnIndex, column, resizer);
                break;

            case GestureStatus.Running:
                UpdateColumnResize(e, columnIndex);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                EndColumnResize(resizer);
                break;
        }
    }

    private void StartColumnResize(PanUpdatedEventArgs e, int columnIndex, ColumnDefinition column, Grid? resizer)
    {
        _startX = e.TotalX;
        _initialColumnWidth = GetColumnActualWidth(columnIndex);
        _currentResizingColumn = column;
        _currentColumnIndex = columnIndex;

        SetResizerLineColor(resizer, Colors.White);
    }

    private void UpdateColumnResize(PanUpdatedEventArgs e, int columnIndex)
    {
        if (_currentColumnIndex != columnIndex || _currentResizingColumn == null) return;

        double delta = e.TotalX - _startX;
        double newWidth = Math.Max(MIN_COLUMN_WIDTH, _initialColumnWidth + delta);

        _currentResizingColumn.Width = new GridLength(newWidth, GridUnitType.Absolute);
        RefreshTableLayout();
    }

    private void EndColumnResize(Grid? resizer)
    {
        SetResizerLineColor(resizer, Color.FromArgb("#777777"));
        RefreshTableLayout();
        ResetResizeState();
    }

    private void SetResizerLineColor(Grid? resizer, Color color)
    {
        if (resizer?.Children.FirstOrDefault() is BoxView line)
        {
            line.BackgroundColor = color;
        }
    }

    private void RefreshTableLayout()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TableHeader?.InvalidateMeasure();
            ObjectsCollectionView?.InvalidateMeasure();
        });
    }

    private void ResetResizeState()
    {
        _currentResizingColumn = null;
        _currentColumnIndex = -1;
    }

    private double GetColumnActualWidth(int columnIndex)
    {
        foreach (var child in TableHeader.Children)
        {
            if (child is View view && Grid.GetColumn(view) == columnIndex)
            {
                return view.Width;
            }
        }
        return _currentResizingColumn?.Width.Value ?? 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_isFirstAppearance)
        {
            _isFirstAppearance = false;
            _viewModel.ForceLoadingOverlay();
        }
        
        SetupButtonAnimations();
    }

    private void SetupButtonAnimations()
    {
        foreach (var button in _animatedButtons.Where(b => b != null))
        {
            UnsubscribeButtonEvents(button);
            SubscribeButtonEvents(button);
        }
    }

    private void SubscribeButtonEvents(Button button)
    {
        button.Pressed += OnButtonPressed;
        button.Released += OnButtonReleased;
    }

    private void UnsubscribeButtonEvents(Button button)
    {
        button.Pressed -= OnButtonPressed;
        button.Released -= OnButtonReleased;
    }

    private void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            _ = Task.Run(async () =>
            {
                await button.ScaleTo(SCALE_PRESSED, ANIMATION_DURATION, Easing.CubicInOut);
                await button.FadeTo(FADE_PRESSED, ANIMATION_DURATION, Easing.CubicInOut);
            });
        }
    }

    private void OnButtonReleased(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            _ = Task.Run(async () =>
            {
                await button.ScaleTo(1.0, ANIMATION_DURATION, Easing.CubicInOut);
                await button.FadeTo(1.0, ANIMATION_DURATION, Easing.CubicInOut);
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CleanupResources();
    }

    private void CleanupResources()
    {
        Loaded -= OnPageLoaded;
        ResetResizeState();

        foreach (var button in _animatedButtons.Where(b => b != null))
        {
            UnsubscribeButtonEvents(button);
        }
    }

    private void OnAscendingCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            _viewModel.ToggleSortDirectionCommand.Execute("true");
        }
    }

    private void OnDescendingCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            _viewModel.ToggleSortDirectionCommand.Execute("false");
        }
    }

    private void Entry_RecordsToLoad_Completed(object sender, EventArgs e)
    {
        _ = Task.Run(() => _viewModel.UpdatePaginationAsync());
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (TableHeader?.Width > 0)
        {
            RefreshTableLayout();
        }
    }
}