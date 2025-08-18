using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oracle_Version_Control.Services;
using Oracle_Version_Control.Views;
using Microsoft.Maui.Dispatching;
using System.Windows.Input;

namespace Oracle_Version_Control.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly OracleService _oracleService;
        private readonly SettingsService _settingsService;

        [ObservableProperty]
        private string _username = "";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _dataSource = "";

        [ObservableProperty]
        private string _tnsPath = "";

        [ObservableProperty]
        private bool _saveCredentials = true;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _autoLogin;

        public LoginViewModel(OracleService oracleService, SettingsService settingsService)
        {
            _oracleService = oracleService ?? throw new ArgumentNullException(nameof(oracleService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            
            try
            {
                LoadSettings();

                if (AutoLogin)
                {
                    LoginCommand.Execute(null);
                }
            }
            catch (Exception)
            {
                Username = "e2desenv";
                DataSource = "SRV_DESENVE2";
                TnsPath = @"C:\app\product\11.2.0\client_1\network\admin";
                SaveCredentials = true;
            }
        }

        private void LoadSettings()
        {
            Username = _settingsService.GetSetting("Username", "e2desenv");
            Password = _settingsService.GetSetting("Password", ""); 
            DataSource = _settingsService.GetSetting("DataSource", "SRV_DESENVE2");
            TnsPath = _settingsService.GetSetting("TnsPath", @"C:\app\product\11.2.0\client_1\network\admin");
            
            bool.TryParse(_settingsService.GetSetting("SaveCredentials", "true"), out bool saveCredentials);
            SaveCredentials = saveCredentials;

            bool.TryParse(_settingsService.GetSetting("AutoLogin", "false"), out bool autoLogin);
            AutoLogin = autoLogin;
        }

        private void SaveSettings()
        {
            if (SaveCredentials)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(Password))
                    {
                        _settingsService.SetSetting("Username", Username ?? string.Empty);
                        _settingsService.SetSetting("Password", Password ?? string.Empty);
                        _settingsService.SetSetting("DataSource", DataSource ?? string.Empty);
                        _settingsService.SetSetting("TnsPath", TnsPath ?? string.Empty);
                        _settingsService.SetSetting("SaveCredentials", SaveCredentials.ToString().ToLower());
                        _settingsService.SetSetting("AutoLogin", AutoLogin.ToString().ToLower());
                        _settingsService.SaveSettings();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || 
                string.IsNullOrWhiteSpace(Password) || 
                string.IsNullOrWhiteSpace(DataSource) ||
                string.IsNullOrWhiteSpace(TnsPath))
            {
                ErrorMessage = "Todos os campos são obrigatórios";
                HasError = true;
                return;
            }

            IsBusy = true;
            HasError = false;

            try
            {
                var result = await _oracleService.ConnectAsync(Username, Password, DataSource, TnsPath);
                
                if (result)
                {
                    SaveSettings();

                    var mainViewModel = new MainViewModel(_oracleService);
                    
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            if (Application.Current != null && Application.Current.Windows.Count > 0)
                            {
                                var window = Application.Current.Windows[0];
                                
                                if (window.Page is NavigationPage navPage)
                                {
                                    await navPage.PushAsync(new MainPage(mainViewModel));
                                }
                                else
                                {
                                    var mainPage = new MainPage(mainViewModel);
                                    window.Page = new NavigationPage(mainPage);
                                }
                            }
                            else
                            {
                                ErrorMessage = "Erro de navegação: não foi possível encontrar a janela principal";
                                HasError = true;
                            }
                        }
                        catch (Exception navEx)
                        {
                            ErrorMessage = $"Erro de navegação: {navEx.Message}";
                            HasError = true;
                            IsBusy = false;
                        }
                    });
                }
                else
                {
                    ErrorMessage = "Falha na conexão. Verifique suas credenciais e Data Source.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erro: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
