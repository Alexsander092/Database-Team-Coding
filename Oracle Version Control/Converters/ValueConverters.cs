using System.Globalization;
using Microsoft.Maui.Controls;

namespace Oracle_Version_Control.Converters
{
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
                
            bool isEqual = value.ToString() == parameter.ToString();
            
            // Se estiver retornando uma cor (para o fundo ou borda)
            if (targetType == typeof(Color) || targetType == typeof(Microsoft.Maui.Graphics.Color))
            {
                if (isEqual)
                    return Color.FromArgb("#E3F2FD"); // Azul claro para o fundo
                else
                    return Color.FromArgb("#FFFFFF"); // Branco para n�o selecionado
            }
            
            // Se retornar Brush (para borda)
            if (targetType == typeof(Brush))
            {
                if (isEqual)
                    return new SolidColorBrush(Color.FromArgb("#0072C6")); // Azul para a borda
                else
                    return new SolidColorBrush(Colors.Transparent); // Transparente para n�o selecionado
            }

            return isEqual;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
                
            return !string.IsNullOrEmpty(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool result = boolValue;
                
                // Se o par�metro for "invert", inverte o valor booleano
                if (parameter is string strParam && strParam.ToLower() == "invert")
                {
                    result = !result;
                }
                
                return result;
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SelectedItemStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;
                
            bool isSelected = value.ToString() == parameter.ToString();
            
            if (targetType == typeof(Color) || targetType == typeof(Microsoft.Maui.Graphics.Color))
            {
                return isSelected ? Color.FromArgb("#E3F2FD") : Colors.White;
            }
            
            if (targetType == typeof(FontAttributes))
            {
                return isSelected ? FontAttributes.Bold : FontAttributes.None;
            }
            
            if (targetType == typeof(Thickness))
            {
                return isSelected ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            }
            
            return isSelected;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AlternatingRowColorConverter : IValueConverter
    {
        // Cores alternadas para as linhas da tabela - preto e cinza escuro conforme solicitado
        private static readonly Color EvenRowColor = Color.FromArgb("#212121"); // Preto
        private static readonly Color OddRowColor = Color.FromArgb("#404040");  // Cinza escuro
        
        // Cores do texto para as linhas alternadas
        private static readonly Color EvenRowTextColor = Colors.White; // Texto branco para fundos escuros
        private static readonly Color OddRowTextColor = Colors.White;
        
        // Dicion�rio para armazenar o �ndice de cada objeto
        private static readonly Dictionary<object, int> _objectIndices = new Dictionary<object, int>();
        private static int _nextIndex = 0;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return EvenRowColor;
                
            // Se estamos resetando os �ndices (�til quando a fonte de dados � recarregada)
            if (parameter is string paramString && paramString == "reset")
            {
                _objectIndices.Clear();
                _nextIndex = 0;
                return EvenRowColor;
            }
            
            // Verificar se o par�metro � para cor de texto
            if (parameter is string textParam && textParam == "TextColor")
            {
                // Obter ou atribuir um �ndice para este objeto
                if (!_objectIndices.TryGetValue(value, out int index))
                {
                    index = _nextIndex++;
                    _objectIndices[value] = index;
                    
                    // Limitar o tamanho do dicion�rio para evitar vazamentos de mem�ria
                    if (_objectIndices.Count > 1000)
                    {
                        _objectIndices.Clear();
                        _nextIndex = 0;
                        _objectIndices[value] = index;
                    }
                }
                
                // Retornar a cor do texto com base no �ndice (branco para ambos os fundos escuros)
                return index % 2 == 0 ? EvenRowTextColor : OddRowTextColor;
            }
            
            // Obter ou atribuir um �ndice para este objeto
            if (!_objectIndices.TryGetValue(value, out int rowIndex))
            {
                rowIndex = _nextIndex++;
                _objectIndices[value] = rowIndex;
                
                // Limitar o tamanho do dicion�rio para evitar vazamentos de mem�ria
                if (_objectIndices.Count > 1000)
                {
                    _objectIndices.Clear();
                    _nextIndex = 0;
                    _objectIndices[value] = rowIndex;
                }
            }
            
            // Alternar cores com base no �ndice
            return rowIndex % 2 == 0 ? EvenRowColor : OddRowColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DirectionSymbolConverter : IValueConverter
    {
        // Caracteres de tri�ngulos pretos (s�lidos) para indicadores de ordena��o
        private static readonly string BLACK_UP_TRIANGLE = "\u25B2"; // ? (U+25B2 BLACK UP-POINTING TRIANGLE)
        private static readonly string BLACK_DOWN_TRIANGLE = "\u25BC"; // ? (U+25BC BLACK DOWN-POINTING TRIANGLE)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Se o valor for um booleano (true = ascendente, false = descendente)
            if (value is bool isAscending)
            {
                // Retornar o tri�ngulo apropriado baseado na dire��o de ordena��o
                return isAscending ? BLACK_UP_TRIANGLE : BLACK_DOWN_TRIANGLE;
            }
            
            // Se n�o for um booleano, retornar o tri�ngulo para baixo como padr�o (descendente)
            return BLACK_DOWN_TRIANGLE;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Se qualquer valor for null ou n�o for bool, retorna false
            if (values == null || values.Length == 0)
                return false;
                
            // Verifica se todos os valores s�o true
            foreach (var value in values)
            {
                if (value is not bool boolValue || !boolValue)
                    return false;
            }
            
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}