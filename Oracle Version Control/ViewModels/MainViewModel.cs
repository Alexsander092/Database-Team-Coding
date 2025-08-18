using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oracle_Version_Control.Models;
using Oracle_Version_Control.Services;
using System.Collections.ObjectModel;
using System.Data;

namespace Oracle_Version_Control.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly OracleService _oracleService;
        private bool _isInitialLoad = true;
        private const int PROCESSING_BATCH_SIZE = 200;
        private string _lastSearchTerm = string.Empty;
        private Dictionary<string, object> _propertyCache = new Dictionary<string, object>();

        // Pagination support
        private readonly List<VersionedObject> _sourceData = new();
        private int _pageSize = 50;

        [ObservableProperty]
        private ObservableCollection<VersionedObject> _objects = new();

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private string _selectedObjectName = string.Empty;

        [ObservableProperty]
        private string _selectedObjectType = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private Color _statusColor = Colors.Black;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _canCheckout = true;

        [ObservableProperty]
        private bool _canCheckin = false;

        [ObservableProperty]
        private VersionedObject? _selectedObject = null;

        [ObservableProperty]
        private bool _isLoadingMore;

        [ObservableProperty]
        private bool _showLoadMoreButton;

        [ObservableProperty]
        private int _loadedObjectsCount;

        [ObservableProperty]
        private string _recordsToLoad = "50";

        [ObservableProperty]
        private Color _checkoutButtonColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#444444")
            : Color.FromArgb("#CCCCCC");

        [ObservableProperty]
        private Color _checkinButtonColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#444444")
            : Color.FromArgb("#CCCCCC");

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalRecords;

        public bool CanGoPreviousPage => CurrentPage > 1;
        public bool CanGoNextPage => CurrentPage < TotalPages;
        public bool CanGoFirstPage => CurrentPage > 1;
        public bool CanGoLastPage => CurrentPage < TotalPages;

        private int GetPageSize()
        {
            if (string.IsNullOrWhiteSpace(RecordsToLoad))
            {
                return 50; // Valor padrão
            }

            if (int.TryParse(RecordsToLoad, out int pageSize) && pageSize > 0)
            {
                return pageSize;
            }

            return 50; // Fallback para valor padrão
        }

        public bool IsLoadMoreEnabled => !IsLoadingMore;

        private string _currentSortField = "Checkout";
        public string CurrentSortField
        {
            get => _currentSortField;
            set
            {
                if (SetProperty(ref _currentSortField, value))
                {
                    UpdateSortFieldDisplay();
                }
            }
        }

        private bool _sortAscending = false;
        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (SetProperty(ref _sortAscending, value))
                {
                    IsAscendingChecked = value;
                    IsDescendingChecked = !value;
                    UpdateSortFieldDisplay();
                }
            }
        }

        [ObservableProperty]
        private string _currentSortFieldDisplay = "Data de checkout (desc)";

        [ObservableProperty]
        private bool _isSortPopupVisible = false;

        [ObservableProperty]
        private bool _isAscendingChecked = false;

        [ObservableProperty]
        private bool _isDescendingChecked = true;

        public static Dictionary<string, string> SortFields { get; } = new Dictionary<string, string>
        {
            { "Objeto", "Objeto" },
            { "ObjectType", "Tipo do objeto" },
            { "Status", "Status (check-out/livre)" },
            { "Usuario", "Usuário" },
            { "Checkout", "Data de check-out" },
            { "Checkin", "Data de check-in" },
        };

        private string _currentUser = string.Empty;
        public string CurrentUser
        {
            get
            {
                if (string.IsNullOrEmpty(_currentUser))
                {
                    _currentUser = _oracleService.CurrentUser;
                }
                return _currentUser;
            }
        }

        public class SearchColumn
        {
            public string Display { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public bool IsPlaceholder { get; set; } = false;
        }

        // Lista de colunas pesquisáveis com placeholder
        public List<SearchColumn> SearchableColumns { get; } = new()
        {
            new SearchColumn { Display = "Coluna", Value = "", IsPlaceholder = true },
            new SearchColumn { Display = "Objeto", Value = "Objeto" },
            new SearchColumn { Display = "Tipo", Value = "ObjectType" },
            new SearchColumn { Display = "Status", Value = "Status" },
            new SearchColumn { Display = "Usuário", Value = "Usuario" },
            new SearchColumn { Display = "Comentário", Value = "Comments" }
        };

        [ObservableProperty]
        private SearchColumn _selectedSearchColumn;

        private string _lastSearchColumn = "Objeto";

        public MainViewModel(OracleService oracleService)
        {
            _oracleService = oracleService;
            _currentUser = _oracleService.CurrentUser;

            StatusMessage = "Buscando objetos...";
            StatusColor = Colors.Gray;
            IsBusy = true;

            IsAscendingChecked = false;
            IsDescendingChecked = true;

            // Seleciona "Objeto" como padrão
            SelectedSearchColumn = SearchableColumns.FirstOrDefault(x => x.Value == "Objeto") ?? SearchableColumns[1];

            UpdateSortFieldDisplay();
            LoadUserCheckoutObjects();
        }

        public void ForceLoadingOverlay()
        {
            if (_isInitialLoad)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusMessage = "Buscando objetos...";
                    StatusColor = Colors.Gray;
                    IsBusy = true;

                    Task.Delay(50).ContinueWith(_ =>
                    {
                        if (!_isInitialLoad)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                IsBusy = false;
                            });
                        }
                    });
                });
            }
        }

        private T GetOrCalculate<T>(string key, Func<T> calculator)
        {
            if (!_propertyCache.ContainsKey(key))
            {
                _propertyCache[key] = calculator();
            }
            return (T)_propertyCache[key];
        }

        private void InvalidateCache(string key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                _propertyCache.Clear();
            }
            else if (_propertyCache.ContainsKey(key))
            {
                _propertyCache.Remove(key);
            }
        }

        private async void LoadUserCheckoutObjects()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Objects.Clear();
                    LoadedObjectsCount = 0;
                    _sourceData.Clear();
                    CurrentPage = 1;
                });

                await Task.Delay(100);
                
                var data = await _oracleService.GetUserCheckoutObjectsAsync(CurrentUser);
                await LoadAllDataAndPaginate(data);

                _isInitialLoad = false;

                if (_sourceData.Count > 0)
                {
                    StatusMessage = $"Carregados {_sourceData.Count} objetos com checkout para você";
                }
                else
                {
                    StatusMessage = "Você não tem objetos com checkout";
                }
                StatusColor = Color.FromArgb("#FFD600");

                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro: {ex.Message}";
                StatusColor = Colors.Red;
                _isInitialLoad = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAllDataAndPaginate(DataTable data)
        {
            if (data.Rows.Count == 0) 
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Objects.Clear();
                    LoadedObjectsCount = 0;
                    TotalRecords = 0;
                    TotalPages = 1;
                    CurrentPage = 1;
                });
                return;
            }

            // Load all data into source collection
            _sourceData.Clear();
            foreach (DataRow row in data.Rows)
            {
                var obj = new VersionedObject(row);
                _sourceData.Add(obj);
            }

            // Calculate pagination
            _pageSize = GetPageSize();
            TotalRecords = _sourceData.Count;
            TotalPages = (int)Math.Ceiling((double)TotalRecords / _pageSize);
            CurrentPage = 1;

            // Load first page
            await LoadCurrentPage();
        }

        private async Task LoadCurrentPage()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Objects.Clear();
                
                int startIndex = (CurrentPage - 1) * _pageSize;
                int endIndex = Math.Min(startIndex + _pageSize, _sourceData.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    Objects.Add(_sourceData[i]);
                }

                LoadedObjectsCount = Objects.Count;
                OnPropertyChanged(nameof(CanGoPreviousPage));
                OnPropertyChanged(nameof(CanGoNextPage));
                OnPropertyChanged(nameof(CanGoFirstPage));
                OnPropertyChanged(nameof(CanGoLastPage));
            });
        }

        [RelayCommand]
        private async Task GoToPreviousPage()
        {
            if (!CanGoPreviousPage) return;

            CurrentPage--;
            await LoadCurrentPage();
        }

        [RelayCommand]
        private async Task GoToNextPage()
        {
            if (!CanGoNextPage) return;

            CurrentPage++;
            await LoadCurrentPage();
        }

        [RelayCommand]
        private async Task GoToFirstPage()
        {
            if (!CanGoFirstPage) return;

            CurrentPage = 1;
            await LoadCurrentPage();
        }

        [RelayCommand]
        private async Task GoToLastPage()
        {
            if (!CanGoLastPage) return;

            CurrentPage = TotalPages;
            await LoadCurrentPage();
        }

        [RelayCommand]
        private async Task OnRecordsToLoadChanged()
        {
            // When page size changes, recalculate pagination
            if (_sourceData.Count > 0)
            {
                _pageSize = GetPageSize();
                TotalPages = (int)Math.Ceiling((double)TotalRecords / _pageSize);
                CurrentPage = 1;
                await LoadCurrentPage();
            }
        }

        [RelayCommand]
        private async Task Search()
        {
            // Não faz busca se placeholder estiver selecionado
            if (SelectedSearchColumn == null || SelectedSearchColumn.IsPlaceholder)
            {
                StatusMessage = "Selecione uma coluna para pesquisar.";
                StatusColor = Colors.Red;
                return;
            }
            if (SearchTerm == _lastSearchTerm && !string.IsNullOrEmpty(_lastSearchTerm) && SelectedSearchColumn.Value == _lastSearchColumn)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Buscando objetos...";
            StatusColor = Colors.Gray;

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Objects.Clear();
                    LoadedObjectsCount = 0;
                    _sourceData.Clear();
                    CurrentPage = 1;
                });

                string searchValue = SearchTerm;
                string searchColumn = SelectedSearchColumn.Value;
                // Conversão especial para status
                if (searchColumn == "Status")
                {
                    if (searchValue.Contains("check", StringComparison.OrdinalIgnoreCase))
                        searchValue = "Y";
                    else if (searchValue.Contains("livre", StringComparison.OrdinalIgnoreCase))
                        searchValue = "N";
                }

                // Load all results for pagination
                var data = await _oracleService.SearchObjectsByColumnAsync(searchColumn, searchValue, CurrentSortField, SortAscending, int.MaxValue, 0);
                await LoadAllDataAndPaginate(data);

                _lastSearchTerm = SearchTerm;
                _lastSearchColumn = SelectedSearchColumn.Value;
                UpdateSortFieldDisplay();

                StatusMessage = $"Encontrados {TotalRecords} objetos";
                StatusColor = Color.FromArgb("#FFD600");

                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;
            _lastSearchTerm = string.Empty;
            await Search();
            IsRefreshing = false;
        }

        [RelayCommand]
        private async Task ReloadObjects()
        {
            IsBusy = true;
            StatusMessage = "Buscando objetos...";
            StatusColor = Colors.Gray;

            try
            {
                SearchTerm = string.Empty;
                _lastSearchTerm = string.Empty;
                InvalidateCache();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Objects.Clear();
                    LoadedObjectsCount = 0;
                    _sourceData.Clear();
                    CurrentPage = 1;
                });

                // Load all results for pagination
                var data = await _oracleService.SearchObjectsAsync("", CurrentSortField, SortAscending, int.MaxValue, 0);
                await LoadAllDataAndPaginate(data);

                UpdateSortFieldDisplay();

                StatusMessage = $"Lista de objetos recarregada ({TotalRecords} objetos)";
                StatusColor = Color.FromArgb("#FFD600");

                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SortBy(string field)
        {
            StatusMessage = $"Ordenando por {field}...";

            try
            {
                // Verificar se há dados para ordenar
                if (_sourceData.Count == 0)
                {
                    StatusMessage = "Nenhum dado disponível para ordenar.";
                    StatusColor = Colors.Orange;
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                    return;
                }

                // Se a coluna do picker for diferente do campo de ordenação, atualiza o campo de ordenação
                if (CurrentSortField != field)
                {
                    CurrentSortField = field;
                }
                else
                {
                    SortAscending = !SortAscending;
                }

                UpdateSortFieldDisplay();
                IsSortPopupVisible = false;

                // Apply sorting to source data and reload current page
                await ApplySortingToSourceData(field);

                StatusMessage = $"Ordenado por {(SortFields.ContainsKey(CurrentSortField) ? SortFields[CurrentSortField] : CurrentSortField)} - {_sourceData.Count} itens";
                StatusColor = Color.FromArgb("#FFD600");
                await Task.Delay(1500);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao ordenar: {ex.Message}";
                StatusColor = Colors.Red;
                IsBusy = false;
                
                // Tentar recarregar dados atuais em caso de erro
                try
                {
                    await LoadCurrentPage();
                }
                catch (Exception loadEx)
                {
                    StatusMessage = $"Erro crítico: {loadEx.Message}";
                }
            }
        }

        private async Task ApplySortingToSourceData(string field)
        {
            if (_sourceData.Count == 0) return; // Não faz nada se não há dados

            try
            {
                // Apply sorting to source data
                var propertyInfo = typeof(VersionedObject).GetProperty(field);
                if (propertyInfo != null)
                {
                    IEnumerable<VersionedObject> sorted;
                    if (SortAscending)
                        sorted = _sourceData.OrderBy(o => propertyInfo.GetValue(o, null));
                    else
                        sorted = _sourceData.OrderByDescending(o => propertyInfo.GetValue(o, null));

                    // Criar nova lista ao invés de limpar e adicionar
                    var sortedList = sorted.ToList();
                    _sourceData.Clear();
                    _sourceData.AddRange(sortedList);
                }
                else
                {
                    // Fallback for older field mapping
                    IEnumerable<VersionedObject> sorted = field switch
                    {
                        "Objeto" => SortAscending ? _sourceData.OrderBy(o => o.Objeto) : _sourceData.OrderByDescending(o => o.Objeto),
                        "ObjectType" => SortAscending ? _sourceData.OrderBy(o => o.ObjectType) : _sourceData.OrderByDescending(o => o.ObjectType),
                        "Status" => SortAscending ? _sourceData.OrderBy(o => o.Status) : _sourceData.OrderByDescending(o => o.Status),
                        "Usuario" => SortAscending ? _sourceData.OrderBy(o => o.Usuario) : _sourceData.OrderByDescending(o => o.Usuario),
                        "Checkout" => SortAscending ? _sourceData.OrderBy(o => o.Checkout) : _sourceData.OrderByDescending(o => o.Checkout),
                        "Checkin" => SortAscending ? _sourceData.OrderBy(o => o.Checkin) : _sourceData.OrderByDescending(o => o.Checkin),
                        _ => _sourceData
                    };

                    // Criar nova lista ao invés de limpar e adicionar
                    var sortedList = sorted.ToList();
                    _sourceData.Clear();
                    _sourceData.AddRange(sortedList);
                }

                // Reset to first page and reload
                CurrentPage = 1;
                await LoadCurrentPage();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao ordenar: {ex.Message}";
                StatusColor = Colors.Red;
                // Em caso de erro, recarrega a página atual para manter os dados
                await LoadCurrentPage();
            }
        }

        [RelayCommand]
        private async Task ToggleSortDirection(string parameter)
        {
            try
            {
                bool targetAscending = true;
                if (bool.TryParse(parameter, out bool result))
                {
                    targetAscending = result;
                }
                else
                {
                    targetAscending = !SortAscending;
                }

                if (SortAscending == targetAscending)
                {
                    return;
                }

                SortAscending = targetAscending;
                UpdateSortFieldDisplay();

                StatusMessage = $"Alterando direção de ordenação para {(SortAscending ? "crescente" : "decrescente")}...";

                // Apply sorting to source data
                await ApplySortingToSourceData(CurrentSortField);

                StatusMessage = $"Direção alterada para {(SortAscending ? "crescente" : "decrescente")}";
                StatusColor = Color.FromArgb("#FFD600");

                await Task.Delay(1500);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro: {ex.Message}";
                StatusColor = Colors.Red;
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            try
            {
                StatusMessage = "Finalizando conexão...";
                StatusColor = Colors.Gray;
                IsBusy = true;

                await Task.Delay(800);
                await _oracleService.DisconnectAsync();
                await Task.Delay(200);

                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao fazer logout: {ex.Message}";
                StatusColor = Colors.Red;
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DownloadObject()
        {
            if (SelectedObject == null || string.IsNullOrWhiteSpace(SelectedObject.Objeto) || string.IsNullOrWhiteSpace(SelectedObject.ObjectType))
            {
                StatusMessage = "Selecione um objeto para download.";
                StatusColor = Colors.Red;
                return;
            }
            try
            {
                IsBusy = true;
                StatusMessage = "Baixando DDL do objeto...";
                StatusColor = Colors.Gray;
                var ddl = await _oracleService.GetObjectDdlAsync(SelectedObject.ObjectType, SelectedObject.Objeto);
                if (string.IsNullOrWhiteSpace(ddl))
                {
                    StatusMessage = "DDL não encontrado para o objeto.";
                    StatusColor = Colors.Red;
                    return;
                }
                var fileName = $"{SelectedObject.Objeto}_{SelectedObject.ObjectType}.sql";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, fileName);
                System.IO.File.WriteAllText(filePath, ddl);
                StatusMessage = $"Arquivo salvo em: {filePath}";
                StatusColor = Color.FromArgb("#FFD600");
                await App.Current.MainPage.DisplayAlert("Atenção", StatusMessage, "OK");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao baixar DDL: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            if (IsLoadingMore)
            {
                return;
            }

            IsLoadingMore = true;
            StatusMessage = "Carregando objetos...";
            StatusColor = Colors.Gray;

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Objects.Clear();
                    LoadedObjectsCount = 0;
                    _sourceData.Clear();
                    CurrentPage = 1;
                });
                
                await Task.Delay(50);

                var data = await _oracleService.SearchObjectsAsync(SearchTerm, CurrentSortField, SortAscending, int.MaxValue, 0);
                await LoadAllDataAndPaginate(data);
                    
                StatusMessage = $"Carregados {TotalRecords} objetos";
                StatusColor = Color.FromArgb("#FFD600");
                
                await Task.Delay(1000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        private async Task Checkout()
        {
            if (string.IsNullOrWhiteSpace(SelectedObjectName))
            {
                StatusMessage = "Informe o nome do objeto";
                StatusColor = Colors.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(Comment))
            {
                StatusMessage = "O comentário é obrigatório para check-out";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                await _oracleService.CheckoutObjectAsync(SelectedObjectName, SelectedObjectType, Comment);
                StatusMessage = $"Check-out realizado para {SelectedObjectName}";
                StatusColor = Color.FromArgb("#FFD600");

                _lastSearchTerm = string.Empty;
                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    LoadUserCheckoutObjects();
                }
                else
                {
                    await Search();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro no check-out: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Checkin()
        {
            if (string.IsNullOrWhiteSpace(SelectedObjectName))
            {
                StatusMessage = "Informe o nome do objeto";
                StatusColor = Colors.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(Comment))
            {
                StatusMessage = "O comentário é obrigatório para check-in";
                StatusColor = Colors.Red;
                return;
            }

            IsBusy = true;
            try
            {
                await _oracleService.CheckinObjectAsync(SelectedObjectName, SelectedObjectType, Comment);
                StatusMessage = $"Check-in realizado para {SelectedObjectName}";
                StatusColor = Color.FromArgb("#FFD600");

                _lastSearchTerm = string.Empty;
                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    LoadUserCheckoutObjects();
                }
                else
                {
                    await Search();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro no check-in: {ex.Message}";
                StatusColor = Colors.Red;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void SelectObject(VersionedObject obj)
        {
            if (obj == null) return;

            SelectedObject = obj;
            SelectedObjectName = obj.Objeto;
            Comment = obj.Comments;
            SelectedObjectType = obj.ObjectType;
           
            CanCheckin = obj.Status == "Y" && obj.Usuario.ToUpper() == CurrentUser;
            CanCheckout = obj.Status != "Y";

            if (!CanCheckout)
            {
                CheckoutButtonColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#444444")
                    : Color.FromArgb("#CCCCCC");
            }
            else
            {
                CheckoutButtonColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#4CAF50")
                    : Color.FromArgb("#98FC55");
            }

            if (!CanCheckin)
            {
                CheckinButtonColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#444444")
                    : Color.FromArgb("#CCCCCC");
            }
            else
            {
                CheckinButtonColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#F44336")
                    : Color.FromArgb("#F44336");
            }
        }

        private void UpdateSortFieldDisplay()
        {
            string cacheKey = $"SortDisplay_{CurrentSortField}_{SortAscending}";

            CurrentSortFieldDisplay = GetOrCalculate(cacheKey, () =>
            {
                if (SortFields.TryGetValue(CurrentSortField, out string fieldName))
                {
                    return $"{fieldName} ({(SortAscending ? "asc" : "desc")})";
                }
                return $"{CurrentSortField} ({(SortAscending ? "asc" : "desc")})";
            });
        }

        [RelayCommand]
        private void ShowSortPopup()
        {
            IsSortPopupVisible = true;
        }

        [RelayCommand]
        private void CloseSortPopup()
        {
            IsSortPopupVisible = false;
        }
    }
}