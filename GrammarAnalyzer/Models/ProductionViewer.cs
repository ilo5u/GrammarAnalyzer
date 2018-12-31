using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarAnalyzer.Models
{
    public class ProductionViewer
    {
        public TokenViewer Nonterminal { get; set; }
        public List<TokenViewer> Candidates { get; set; }
    }
}
