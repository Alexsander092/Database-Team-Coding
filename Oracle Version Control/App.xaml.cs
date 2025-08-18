using Oracle_Version_Control.Views;

namespace Oracle_Version_Control;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        try
        {
            InitializeComponent();

            try
            {
                var navigationPage = new NavigationPage(loginPage);
                MainPage = navigationPage;
            }
            catch (Exception ex)
            {
                try
                {
                    MainPage = new ContentPage
                    {
                        Content = new VerticalStackLayout
                        {
                            Children = {
                                new Label { Text = "Erro ao iniciar o aplicativo", FontSize = 20, HorizontalOptions = LayoutOptions.Center },
                                new Label { Text = ex.Message, FontSize = 16, Margin = new Thickness(20) }
                            },
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                }
                catch
                {
                }
            }
        }
        catch (Exception)
        {
        }
    }
}