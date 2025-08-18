using Oracle_Version_Control.ViewModels;

namespace Oracle_Version_Control.Views;

public partial class MainPage : ContentPage
{
    private MainViewModel _viewModel;
    private bool _isFirstAppearance = true;
    private double _initialColumnWidth;
    private ColumnDefinition _currentResizingColumn;
    private int _currentColumnIndex = -1;
    private double _startX;
    private double _minColumnWidth = 50;
    private double _resizerStartPosition;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, EventArgs e)
    {
        SetupColumnResizers();
    }

    private void SetupColumnResizers()
    {
        var resizers = new[] { "Col1Resizer", "Col2Resizer", "Col3Resizer", "Col4Resizer", "Col5Resizer" };

        foreach (var resizerName in resizers)
        {
            var resizer = FindByName(resizerName) as Grid;
            if (resizer != null)
            {
                resizer.WidthRequest = 10;
                resizer.GestureRecognizers.Clear();

                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += (s, e) => OnColumnResizerPanUpdated(s, e, resizerName);
                resizer.GestureRecognizers.Add(panGesture);
            }
        }
    }

    private void OnColumnResizerPanUpdated(object sender, PanUpdatedEventArgs e, string resizerName)
    {
        int columnIndex = int.Parse(resizerName.Substring(3, 1)) - 1;
        var column = TableHeader.ColumnDefinitions[columnIndex];
        var resizer = FindByName(resizerName) as Grid;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _resizerStartPosition = GetResizerAbsolutePosition(resizer);
                _startX = e.TotalX;
                _initialColumnWidth = GetColumnActualWidth(columnIndex);
                _currentResizingColumn = column;
                _currentColumnIndex = columnIndex;

                if (resizer != null)
                {
                    var line = resizer.Children[0] as BoxView;
                    if (line != null)
                    {
                        line.BackgroundColor = Colors.White;
                    }
                }
                break;

            case GestureStatus.Running:
                if (_currentColumnIndex == columnIndex && _currentResizingColumn != null)
                {
                    double delta = e.TotalX - _startX;
                    double newWidth = Math.Max(_minColumnWidth, _initialColumnWidth + delta);

                    _currentResizingColumn.Width = new GridLength(newWidth, GridUnitType.Absolute);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TableHeader.InvalidateMeasure();
                        ObjectsCollectionView.InvalidateMeasure();
                    });
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (resizer != null)
                {
                    var line = resizer.Children[0] as BoxView;
                    if (line != null)
                    {
                        line.BackgroundColor = Color.FromArgb("#777777");
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TableHeader.InvalidateMeasure();
                    ObjectsCollectionView.InvalidateMeasure();
                });

                _currentResizingColumn = null;
                _currentColumnIndex = -1;
                break;
        }
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

    private double GetResizerAbsolutePosition(Grid resizer)
    {
        if (resizer == null) return 0;

        double position = 0;
        var current = resizer.Parent as VisualElement;

        while (current != null && current != TableHeader)
        {
            position += current.X;
            current = current.Parent as VisualElement;
        }

        return position + resizer.X + resizer.Width;
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
        if (CheckoutButton != null)
        {
            CheckoutButton.Pressed += OnButtonPressed;
            CheckoutButton.Released += OnButtonReleased;
        }

        if (CheckinButton != null)
        {
            CheckinButton.Pressed += OnButtonPressed;
            CheckinButton.Released += OnButtonReleased;
        }

        if (LogoutButton != null)
        {
            LogoutButton.Pressed -= OnButtonPressed;
            LogoutButton.Released -= OnButtonReleased;

            LogoutButton.Pressed += OnButtonPressed;
            LogoutButton.Released += OnButtonReleased;
        }

        if (ReloadButton != null)
        {
            ReloadButton.Pressed += OnButtonPressed;
            ReloadButton.Released += OnButtonReleased;
        }

        if (FirstPageButton != null)
        {
            FirstPageButton.Pressed += OnButtonPressed;
            FirstPageButton.Released += OnButtonReleased;
        }

        if (PreviousPageButton != null)
        {
            PreviousPageButton.Pressed += OnButtonPressed;
            PreviousPageButton.Released += OnButtonReleased;
        }

        if (NextPageButton != null)
        {
            NextPageButton.Pressed += OnButtonPressed;
            NextPageButton.Released += OnButtonReleased;
        }

        if (LastPageButton != null)
        {
            LastPageButton.Pressed += OnButtonPressed;
            LastPageButton.Released += OnButtonReleased;
        }


    }

    private void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.ScaleTo(0.95, 100, Easing.CubicInOut);
            button.FadeTo(0.9, 100, Easing.CubicInOut);
        }
    }

    private void OnButtonReleased(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.ScaleTo(1.0, 100, Easing.CubicInOut);
            button.FadeTo(1.0, 100, Easing.CubicInOut);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        Loaded -= OnPageLoaded;

        _currentColumnIndex = -1;
        _currentResizingColumn = null;

        if (CheckoutButton != null)
        {
            CheckoutButton.Pressed -= OnButtonPressed;
            CheckoutButton.Released -= OnButtonReleased;
        }

        if (CheckinButton != null)
        {
            CheckinButton.Pressed -= OnButtonPressed;
            CheckinButton.Released -= OnButtonReleased;
        }

        if (LogoutButton != null)
        {
            LogoutButton.Pressed -= OnButtonPressed;
            LogoutButton.Released -= OnButtonReleased;
        }

        if (ReloadButton != null)
        {
            ReloadButton.Pressed -= OnButtonPressed;
            ReloadButton.Released -= OnButtonReleased;
        }

        if (FirstPageButton != null)
        {
            FirstPageButton.Pressed -= OnButtonPressed;
            FirstPageButton.Released -= OnButtonReleased;
        }

        if (PreviousPageButton != null)
        {
            PreviousPageButton.Pressed -= OnButtonPressed;
            PreviousPageButton.Released -= OnButtonReleased;
        }

        if (NextPageButton != null)
        {
            NextPageButton.Pressed -= OnButtonPressed;
            NextPageButton.Released -= OnButtonReleased;
        }

        if (LastPageButton != null)
        {
            LastPageButton.Pressed -= OnButtonPressed;
            LastPageButton.Released -= OnButtonReleased;
        }
    }

    private void OnAscendingCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_viewModel != null && e.Value)
        {
            _viewModel.ToggleSortDirectionCommand.Execute("true");
        }
    }

    private void OnDescendingCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_viewModel != null && e.Value)
        {
            _viewModel.ToggleSortDirectionCommand.Execute("false");
        }
    }

    private void Entry_RecordsToLoad_Completed(object sender, EventArgs e)
    {
        // When page size changes, trigger pagination recalculation
        _viewModel?.LoadMoreCommand?.Execute(null);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (TableHeader != null && TableHeader.Width > 0)
        {
            TableHeader.InvalidateMeasure();
            ObjectsCollectionView.InvalidateMeasure();
        }
    }
}