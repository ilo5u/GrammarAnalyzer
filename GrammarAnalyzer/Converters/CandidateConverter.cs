using GrammarAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace GrammarAnalyzer.Converters
{
    class CandidateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            List<TokenViewer> tokens = (List<TokenViewer>)value;
            string candidate = "";
            foreach (var item in tokens)
            {
                candidate += item.Token;
            }
            return candidate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
