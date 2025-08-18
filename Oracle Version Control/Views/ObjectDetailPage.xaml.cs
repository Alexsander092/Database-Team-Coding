using Oracle_Version_Control.ViewModels;

namespace Oracle_Version_Control.Views;

public partial class ObjectDetailPage : ContentPage
{
    public ObjectDetailPage(ObjectDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}