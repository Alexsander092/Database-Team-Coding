using Oracle_Version_Control.ViewModels;

namespace Oracle_Version_Control.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        LoginButton.Pressed += OnLoginButtonPressed;
        LoginButton.Released += OnLoginButtonReleased;
    }

    private void OnLoginButtonPressed(object sender, EventArgs e)
    {
        LoginButton.ScaleTo(0.95, 100, Easing.CubicInOut);
    }

    private void OnLoginButtonReleased(object sender, EventArgs e)
    {
        LoginButton.ScaleTo(1.0, 100, Easing.CubicInOut);
    }

    private void OnPasswordCompleted(object sender, EventArgs e)
    {
        if (_viewModel.LoginCommand.CanExecute(null))
        {
            _viewModel.LoginCommand.Execute(null);
        }
    }
}