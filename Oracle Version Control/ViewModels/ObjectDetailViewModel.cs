using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oracle_Version_Control.Services;
using System.Data;

namespace Oracle_Version_Control.ViewModels
{
    [QueryProperty(nameof(ObjectName), "objectName")]
    public partial class ObjectDetailViewModel : ObservableObject
    {
        private readonly OracleService _oracleService;

        [ObservableProperty]
        private string _objectName = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _user = string.Empty;

        [ObservableProperty]
        private string _checkoutTime = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private Color _statusColor = Colors.Black;

        [ObservableProperty]
        private string _objectType = string.Empty;

        public ObjectDetailViewModel(OracleService oracleService)
        {
            _oracleService = oracleService;
        }

        public async Task Initialize()
        {
            await LoadObjectDetails();
        }

        partial void OnObjectNameChanged(string value)
        {
            _ = Initialize();
        }

        private async Task LoadObjectDetails()
        {
            try
            {
                var data = await _oracleService.GetObjectStatusAsync(ObjectName);

                if (data.Rows.Count > 0)
                {
                    var row = data.Rows[0];
                    Status = row["Status"].ToString() == "Y" ? "Check-out" : "Livre";
                    User = row["Usuario"]?.ToString() ?? string.Empty;
                    CheckoutTime = row["Checkout"]?.ToString() ?? string.Empty;
                    StatusColor = row["Status"].ToString() == "Y" ? Colors.Red : Colors.Green;
                }
            }
            catch (Exception)
            {
            }
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
