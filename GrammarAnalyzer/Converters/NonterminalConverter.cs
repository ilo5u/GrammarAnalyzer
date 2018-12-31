using GrammarAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace GrammarAnalyzer.Converters
{
    class NonterminalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            TokenViewer token = (TokenViewer)value;
            return token.Token;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
