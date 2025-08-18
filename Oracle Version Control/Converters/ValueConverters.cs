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
                    return Color.FromArgb("#FFFFFF"); // Branco para não selecionado
            }
            
            // Se retornar Brush (para borda)
            if (targetType == typeof(Brush))
            {
                if (isEqual)
                    return new SolidColorBrush(Color.FromArgb("#0072C6")); // Azul para a borda
                else
                    return new SolidColorBrush(Colors.Transparent); // Transparente para não selecionado
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
                
                // Se o parâmetro for "invert", inverte o valor booleano
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
        
        // Dicionário para armazenar o índice de cada objeto
        private static readonly Dictionary<object, int> _objectIndices = new Dictionary<object, int>();
        private static int _nextIndex = 0;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return EvenRowColor;
                
            // Se estamos resetando os índices (útil quando a fonte de dados é recarregada)
            if (parameter is string paramString && paramString == "reset")
            {
                _objectIndices.Clear();
                _nextIndex = 0;
                return EvenRowColor;
            }
            
            // Verificar se o parâmetro é para cor de texto
            if (parameter is string textParam && textParam == "TextColor")
            {
                // Obter ou atribuir um índice para este objeto
                if (!_objectIndices.TryGetValue(value, out int index))
                {
                    index = _nextIndex++;
                    _objectIndices[value] = index;
                    
                    // Limitar o tamanho do dicionário para evitar vazamentos de memória
                    if (_objectIndices.Count > 1000)
                    {
                        _objectIndices.Clear();
                        _nextIndex = 0;
                        _objectIndices[value] = index;
                    }
                }
                
                // Retornar a cor do texto com base no índice (branco para ambos os fundos escuros)
                return index % 2 == 0 ? EvenRowTextColor : OddRowTextColor;
            }
            
            // Obter ou atribuir um índice para este objeto
            if (!_objectIndices.TryGetValue(value, out int rowIndex))
            {
                rowIndex = _nextIndex++;
                _objectIndices[value] = rowIndex;
                
                // Limitar o tamanho do dicionário para evitar vazamentos de memória
                if (_objectIndices.Count > 1000)
                {
                    _objectIndices.Clear();
                    _nextIndex = 0;
                    _objectIndices[value] = rowIndex;
                }
            }
            
            // Alternar cores com base no índice
            return rowIndex % 2 == 0 ? EvenRowColor : OddRowColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DirectionSymbolConverter : IValueConverter
    {
        // Caracteres de triângulos pretos (sólidos) para indicadores de ordenação
        private static readonly string BLACK_UP_TRIANGLE = "\u25B2"; // ? (U+25B2 BLACK UP-POINTING TRIANGLE)
        private static readonly string BLACK_DOWN_TRIANGLE = "\u25BC"; // ? (U+25BC BLACK DOWN-POINTING TRIANGLE)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Se o valor for um booleano (true = ascendente, false = descendente)
            if (value is bool isAscending)
            {
                // Retornar o triângulo apropriado baseado na direção de ordenação
                return isAscending ? BLACK_UP_TRIANGLE : BLACK_DOWN_TRIANGLE;
            }
            
            // Se não for um booleano, retornar o triângulo para baixo como padrão (descendente)
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
            // Se qualquer valor for null ou não for bool, retorna false
            if (values == null || values.Length == 0)
                return false;
                
            // Verifica se todos os valores são true
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