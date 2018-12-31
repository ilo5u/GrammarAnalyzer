using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarAnalyzer.Models
{
    public enum TokenType
    {
        Epsilon,
        Terminal,
        Nonterminal
    }
    public class TokenViewer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _token;
        private bool _isStart;
        public string Token
        {
            get
            {
                return _token;
            }

            set
            {
                _token = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Token"));
                }
            }
        }

        public bool IsStart
        {
            get
            {
                return _isStart;
            }

            set
            {
                _isStart = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IsStart"));
                }
            }
        }

        public TokenType Type;
    }
}
