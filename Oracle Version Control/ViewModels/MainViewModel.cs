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

        [ObservableProperty]
        private string _currentSortFieldDisplay = "Data de checkout (desc)";

        [ObservableProperty]
        private bool _isSortPopupVisible = false;

        [ObservableProperty]
        private bool _isAscendingChecked = false;

        [ObservableProperty]
        private bool _isDescendingChecked = true;

        [ObservableProperty]
        private SearchColumn _selectedSearchColumn;

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

        public static Dictionary<string, string> SortFields { get; } = new()
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

        public List<SearchColumn> SearchableColumns { get; } = new()
        {
            new SearchColumn { Display = "Coluna", Value = "", IsPlaceholder = true },
            new SearchColumn { Display = "Objeto", Value = "Objeto" },
            new SearchColumn { Display = "Tipo", Value = "ObjectType" },
            new SearchColumn { Display = "Status", Value = "Status" },
            new SearchColumn { Display = "Usuário", Value = "Usuario" },
            new SearchColumn { Display = "Comentário", Value = "Comments" }
        };

        private string _lastSearchColumn = "Objeto";
        public bool IsLoadMoreEnabled => !IsLoadingMore;

        public MainViewModel(OracleService oracleService)
        {
            _oracleService = oracleService;
            _currentUser = _oracleService.CurrentUser;

            StatusMessage = "Buscando objetos...";
            StatusColor = Colors.Gray;
            IsBusy = true;

            IsAscendingChecked = false;
            IsDescendingChecked = true;

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

        private int GetPageSize()
        {
            if (string.IsNullOrWhiteSpace(RecordsToLoad))
                return 50;

            if (int.TryParse(RecordsToLoad, out int pageSize) && pageSize > 0)
                return pageSize;

            return 50;
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

        private void SetStatusMessage(string message, Color color, int hideAfterMs = 0)
        {
            StatusMessage = message;
            StatusColor = color;
            
            if (hideAfterMs > 0)
            {
                Task.Delay(hideAfterMs).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() => StatusMessage = string.Empty);
                });
            }
        }

        private void ClearDataAndResetPagination()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Objects.Clear();
                LoadedObjectsCount = 0;
                _sourceData.Clear();
                CurrentPage = 1;
            });
        }

        private async void LoadUserCheckoutObjects()
        {
            try
            {
                ClearDataAndResetPagination();
                await Task.Delay(100);
                
                var data = await _oracleService.GetUserCheckoutObjectsAsync(CurrentUser);
                await LoadAllDataAndPaginate(data);

                _isInitialLoad = false;

                if (_sourceData.Count > 0)
                {
                    SetStatusMessage($"Carregados {_sourceData.Count} objetos com checkout para você", Color.FromArgb("#FFD600"), 2000);
                }
                else
                {
                    SetStatusMessage("Você não tem objetos com checkout", Color.FromArgb("#FFD600"), 2000);
                }
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro: {ex.Message}", Colors.Red);
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

            _sourceData.Clear();
            foreach (DataRow row in data.Rows)
            {
                var obj = new VersionedObject(row);
                _sourceData.Add(obj);
            }

            _pageSize = GetPageSize();
            TotalRecords = _sourceData.Count;
            TotalPages = (int)Math.Ceiling((double)TotalRecords / _pageSize);
            CurrentPage = 1;

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
                    var obj = _sourceData[i];
                    obj.DisplayIndex = i - startIndex;
                    Objects.Add(obj);
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

        public async Task UpdatePaginationAsync()
        {
            if (_sourceData.Count > 0)
            {
                IsBusy = true;
                SetStatusMessage("Recalculando paginação...", Colors.Gray);
                
                try
                {
                    await Task.Delay(100); // Pequeno delay para mostrar o overlay
                    
                    _pageSize = GetPageSize();
                    TotalPages = (int)Math.Ceiling((double)TotalRecords / _pageSize);
                    CurrentPage = 1;
                    await LoadCurrentPage();
                    
                    SetStatusMessage($"Paginação atualizada - {_pageSize} registros por página", Color.FromArgb("#FFD600"), 1500);
                }
                catch (Exception ex)
                {
                    SetStatusMessage($"Erro ao atualizar paginação: {ex.Message}", Colors.Red);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task Search()
        {
            if (SelectedSearchColumn?.IsPlaceholder == true)
            {
                SetStatusMessage("Selecione uma coluna para pesquisar.", Colors.Red);
                return;
            }

            if (SearchTerm == _lastSearchTerm && !string.IsNullOrEmpty(_lastSearchTerm) && SelectedSearchColumn.Value == _lastSearchColumn)
                return;

            IsBusy = true;
            SetStatusMessage("Buscando objetos...", Colors.Gray);

            try
            {
                ClearDataAndResetPagination();

                string searchValue = SearchTerm;
                string searchColumn = SelectedSearchColumn.Value;
                
                if (searchColumn == "Status")
                {
                    searchValue = searchValue.Contains("check", StringComparison.OrdinalIgnoreCase) ? "Y" : "N";
                }

                var data = await _oracleService.SearchObjectsByColumnAsync(searchColumn, searchValue, CurrentSortField, SortAscending, int.MaxValue, 0);
                await LoadAllDataAndPaginate(data);

                _lastSearchTerm = SearchTerm;
                _lastSearchColumn = SelectedSearchColumn.Value;
                UpdateSortFieldDisplay();

                SetStatusMessage($"Encontrados {TotalRecords} objetos", Color.FromArgb("#FFD600"), 2000);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro: {ex.Message}", Colors.Red);
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
            SetStatusMessage("Buscando objetos...", Colors.Gray);

            try
            {
                SearchTerm = string.Empty;
                _lastSearchTerm = string.Empty;
                InvalidateCache();

                ClearDataAndResetPagination();

                var data = await _oracleService.SearchObjectsAsync("", CurrentSortField, SortAscending, int.MaxValue, 0);
                await LoadAllDataAndPaginate(data);

                UpdateSortFieldDisplay();
                SetStatusMessage($"Lista de objetos recarregada ({TotalRecords} objetos)", Color.FromArgb("#FFD600"), 2000);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro: {ex.Message}", Colors.Red);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SortBy(string field)
        {
            SetStatusMessage($"Ordenando por {field}...", Colors.Gray);

            try
            {
                if (_sourceData.Count == 0)
                {
                    SetStatusMessage("Nenhum dado disponível para ordenar.", Colors.Orange, 2000);
                    return;
                }

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

                await ApplySortingToSourceData(field);

                string fieldDisplayName = SortFields.TryGetValue(CurrentSortField, out string name) ? name : CurrentSortField;
                SetStatusMessage($"Ordenado por {fieldDisplayName} - {_sourceData.Count} itens", Color.FromArgb("#FFD600"), 1500);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro ao ordenar: {ex.Message}", Colors.Red);
                IsBusy = false;
                
                try
                {
                    await LoadCurrentPage();
                }
                catch (Exception loadEx)
                {
                    SetStatusMessage($"Erro crítico: {loadEx.Message}", Colors.Red);
                }
            }
        }

        private async Task ApplySortingToSourceData(string field)
        {
            if (_sourceData.Count == 0) return;

            try
            {
                var propertyInfo = typeof(VersionedObject).GetProperty(field);
                IEnumerable<VersionedObject> sorted;

                if (propertyInfo != null)
                {
                    sorted = SortAscending 
                        ? _sourceData.OrderBy(o => propertyInfo.GetValue(o, null))
                        : _sourceData.OrderByDescending(o => propertyInfo.GetValue(o, null));
                }
                else
                {
                    sorted = field switch
                    {
                        "Objeto" => SortAscending ? _sourceData.OrderBy(o => o.Objeto) : _sourceData.OrderByDescending(o => o.Objeto),
                        "ObjectType" => SortAscending ? _sourceData.OrderBy(o => o.ObjectType) : _sourceData.OrderByDescending(o => o.ObjectType),
                        "Status" => SortAscending ? _sourceData.OrderBy(o => o.Status) : _sourceData.OrderByDescending(o => o.Status),
                        "Usuario" => SortAscending ? _sourceData.OrderBy(o => o.Usuario) : _sourceData.OrderByDescending(o => o.Usuario),
                        "Checkout" => SortAscending ? _sourceData.OrderBy(o => o.Checkout) : _sourceData.OrderByDescending(o => o.Checkout),
                        "Checkin" => SortAscending ? _sourceData.OrderBy(o => o.Checkin) : _sourceData.OrderByDescending(o => o.Checkin),
                        _ => _sourceData
                    };
                }

                var sortedList = sorted.ToList();
                _sourceData.Clear();
                _sourceData.AddRange(sortedList);

                CurrentPage = 1;
                await LoadCurrentPage();
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro ao ordenar: {ex.Message}", Colors.Red);
                await LoadCurrentPage();
            }
        }

        [RelayCommand]
        private async Task ToggleSortDirection(string parameter)
        {
            try
            {
                bool targetAscending = bool.TryParse(parameter, out bool result) ? result : !SortAscending;

                if (SortAscending == targetAscending)
                    return;

                SortAscending = targetAscending;
                UpdateSortFieldDisplay();

                SetStatusMessage($"Alterando direção de ordenação para {(SortAscending ? "crescente" : "decrescente")}...", Colors.Gray);

                await ApplySortingToSourceData(CurrentSortField);

                SetStatusMessage($"Direção alterada para {(SortAscending ? "crescente" : "decrescente")}", Color.FromArgb("#FFD600"), 1500);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro: {ex.Message}", Colors.Red);
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            try
            {
                SetStatusMessage("Finalizando conexão...", Colors.Gray);
                IsBusy = true;

                await Task.Delay(800);
                await _oracleService.DisconnectAsync();
                await Task.Delay(200);

                await Application.Current.MainPage.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro ao fazer logout: {ex.Message}", Colors.Red);
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task DownloadObject()
        {
            if (SelectedObject?.Objeto == null || string.IsNullOrWhiteSpace(SelectedObject.ObjectType))
            {
                SetStatusMessage("Selecione um objeto para download.", Colors.Red);
                return;
            }

            try
            {
                IsBusy = true;
                SetStatusMessage("Baixando DDL do objeto...", Colors.Gray);
                
                var ddl = await _oracleService.GetObjectDdlAsync(SelectedObject.ObjectType, SelectedObject.Objeto);
                if (string.IsNullOrWhiteSpace(ddl))
                {
                    SetStatusMessage("DDL não encontrado para o objeto.", Colors.Red);
                    return;
                }

                var fileName = $"{SelectedObject.Objeto}_{SelectedObject.ObjectType}.sql";
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, fileName);
                
                System.IO.File.WriteAllText(filePath, ddl);
                
                string message = $"Arquivo salvo em: {filePath}";
                SetStatusMessage(message, Color.FromArgb("#FFD600"));
                await App.Current.MainPage.DisplayAlert("Atenção", message, "OK");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro ao baixar DDL: {ex.Message}", Colors.Red);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadMore()
        {
            if (IsLoadingMore) return;

            IsLoadingMore = true;
            SetStatusMessage("Carregando objetos...", Colors.Gray);

            try
            {
                ClearDataAndResetPagination();
                await Task.Delay(50);

                var data = await _oracleService.SearchObjectsAsync(SearchTerm, CurrentSortField, SortAscending, int.MaxValue, 0);
                await LoadAllDataAndPaginate(data);
                    
                SetStatusMessage($"Carregados {TotalRecords} objetos", Color.FromArgb("#FFD600"), 1000);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Erro: {ex.Message}", Colors.Red);
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        private async Task PerformObjectOperation(Func<Task> operation, string operationName)
        {
            if (string.IsNullOrWhiteSpace(SelectedObjectName))
            {
                SetStatusMessage("Informe o nome do objeto", Colors.Red);
                return;
            }

            if (string.IsNullOrWhiteSpace(Comment))
            {
                SetStatusMessage($"O comentário é obrigatório para {operationName}", Colors.Red);
                return;
            }

            IsBusy = true;
            try
            {
                await operation();
                SetStatusMessage($"{operationName} realizado para {SelectedObjectName}", Color.FromArgb("#FFD600"));

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
                SetStatusMessage($"Erro no {operationName.ToLower()}: {ex.Message}", Colors.Red);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Checkout()
        {
            await PerformObjectOperation(
                () => _oracleService.CheckoutObjectAsync(SelectedObjectName, SelectedObjectType, Comment),
                "check-out"
            );
        }

        [RelayCommand]
        private async Task Checkin()
        {
            await PerformObjectOperation(
                () => _oracleService.CheckinObjectAsync(SelectedObjectName, SelectedObjectType, Comment),
                "check-in"
            );
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

            bool isDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;

            CheckoutButtonColor = CanCheckout 
                ? (isDarkTheme ? Color.FromArgb("#4CAF50") : Color.FromArgb("#98FC55"))
                : (isDarkTheme ? Color.FromArgb("#444444") : Color.FromArgb("#CCCCCC"));

            CheckinButtonColor = CanCheckin 
                ? Color.FromArgb("#F44336")
                : (isDarkTheme ? Color.FromArgb("#444444") : Color.FromArgb("#CCCCCC"));
        }

        private void UpdateSortFieldDisplay()
        {
            string cacheKey = $"SortDisplay_{CurrentSortField}_{SortAscending}";

            CurrentSortFieldDisplay = GetOrCalculate(cacheKey, () =>
            {
                string fieldName = SortFields.TryGetValue(CurrentSortField, out string name) ? name : CurrentSortField;
                return $"{fieldName} ({(SortAscending ? "asc" : "desc")})";
            });
        }

        [RelayCommand]
        private void ShowSortPopup() => IsSortPopupVisible = true;

        [RelayCommand]
        private void CloseSortPopup() => IsSortPopupVisible = false;
    }
}