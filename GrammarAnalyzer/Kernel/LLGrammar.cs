using GrammarAnalyzer.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GrammarAnalyzer.Kernel
{
    public class LLGrammar : Grammar
    {
        private Dictionary<Token, int> _rs = new Dictionary<Token, int>();
        private Dictionary<Token, int> _cs = new Dictionary<Token, int>();
        private Dictionary<KeyValuePair<int, int>, Prodc> _sheet = new Dictionary<KeyValuePair<int, int>, Prodc>();
        private Dictionary<KeyValuePair<int, int>, List<Prodc>> _dpsheet = new Dictionary<KeyValuePair<int, int>, List<Prodc>>();

        private void EliminateLeftRecursion()
        {
            foreach (var token in _nonterms)
            {
                List<List<Token>> alphas = new List<List<Token>>();
                List<List<Token>> betas = new List<List<Token>>();
                foreach (var prodc in _prodcs)
                {
                    if (prodc._right[0].Equals(token))
                    {
                        prodc._right.RemoveAt(0);
                        alphas.Add(prodc._right);
                    }
                    else
                    {
                        betas.Add(prodc._right);
                    }
                }
                Token dot = new Token { _type = token._type, _attr = token._attr };
                if (alphas.Count > 0)
                {
                    do
                    {
                        dot._attr += '\'';
                    } while (_tokens.Contains(dot));
                    _nonterms.Add(dot); _tokens.Add(dot);
                }
                alphas.ForEach(e => e.Add(dot));
                alphas.ForEach(e => InsertProduction(dot._attr, e));

                betas.ForEach(e => e.Add(dot));
                betas.ForEach(e => InsertProduction(token._attr, e));
            }
        }

        private void EliminateCommonLeftTokens()
        {
            bool noCommonTokens = false;
            do
            {
                noCommonTokens = true;
                foreach (var token in _nonterms)
                {
                    List<List<Token>> cans = new List<List<Token>>();
                    foreach (var prodc in _prodcs)
                    {
                        if (prodc._left.Equals(token))
                        {
                            cans.Add(prodc._right);
                        }
                    }
                    List<int> merge = new List<int>();
                    List<int> rest = new List<int>();
                    for (int main = 0; main != cans.Count; ++main)
                    {
                        for (int sub = 0; sub != cans.Count; ++sub)
                        {
                            if (main != sub && cans[main].First().Equals(cans[sub].First()))
                            {
                                merge.Add(sub);
                            }
                            else if (main != sub)
                            {
                                rest.Add(sub);
                            }
                        }
                        if (merge.Count > 0)
                        {
                            merge.Add(main);
                            break;
                        }
                        else
                        {
                            rest.Clear();
                        }
                    }
                    if (merge.Count > 0)
                    {
                        rest.ForEach(e => InsertProduction(token._attr, cans[e]));
                        Token dot = new Token { _type = token._type, _attr = token._attr };
                        do
                        {
                            dot._attr += '\'';
                        } while (_tokens.Contains(dot));
                        _nonterms.Add(dot); _tokens.Add(dot);

                        InsertProduction(token._attr, new List<Token> { cans[merge[0]].First(), dot });
                        merge.ForEach(e =>
                        {
                            cans[e].RemoveAt(0);
                            if (cans[e].Count == 0)
                            {
                                InsertProduction(token._attr, new List<Token> { new Token() });
                            }
                            else
                            {
                                InsertProduction(token._attr, cans[e]);
                            }
                        });
                        noCommonTokens = false;
                    }
                    else
                    {
                        cans.ForEach(e => InsertProduction(token._attr, e));
                    }
                }
            } while (!noCommonTokens);
        }

        private void Extend()
        {
            Token dot = new Token { _type = Token.Type.NONTERMINAL, _attr = _start._attr };
            do
            {
                dot._attr += '\'';
            } while (_tokens.Contains(dot));
            _nonterms.Add(dot); _tokens.Add(dot);

            InsertProduction(dot._attr, new List<Token> { _start });
            _start = dot;
        }

        private Dictionary<Token, List<Token>> GetFirstSet()
        {
            Dictionary<Token, List<Token>> fis = new Dictionary<Token, List<Token>>();
            return fis;
        }

        private Dictionary<Token, List<Token>> GetFollowSet(Dictionary<Token, List<Token>> fis)
        {
            Dictionary<Token, List<Token>> fos = new Dictionary<Token, List<Token>>();
            return fos;
        }

        public LLGrammar(Grammar raw)
        {
            _tokens = raw._tokens;
            _terms = raw._terms;
            _nonterms = raw._nonterms;
            _prodcs = raw._prodcs;
            _start = raw._start;

            EliminateLeftRecursion();
            EliminateCommonLeftTokens();
        }
    }
}
