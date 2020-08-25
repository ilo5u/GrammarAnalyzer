using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace GrammarAnalyzer.Kernel
{
    public class Grammar
    {
        public class Token
        {
            public enum Type
            {
                TERMINAL,
                NONTERMINAL,
                INVALID
            }
            public Type _type;
            public string _attr;

            public Token()
            {
                _type = Type.TERMINAL;
                _attr = "ε";
            }
            
            public Token(Type type)
            {
                _type = type;
            }

            public Token(Type type, string attr)
            {
                _type = type;
                _attr = attr;
            }
        }

        public class Prodc
        {
            public Token _left;
            public List<Token> _right;
        }
        public SortedSet<Token> _tokens = new SortedSet<Token>();
        public SortedSet<Token> _terms = new SortedSet<Token>();
        public SortedSet<Token> _nonterms = new SortedSet<Token>();
        public SortedSet<Prodc> _prodcs = new SortedSet<Prodc>();
        public Token _start = new Token();

        public bool InsertTerminal(string term)
        {
            Token token = new Token { _type = Token.Type.TERMINAL, _attr = term };
            return _tokens.Add(token) && _terms.Add(token);
        }

        public bool InsertNonterminal(string nonterm)
        {
            Token token = new Token { _type = Token.Type.NONTERMINAL, _attr = nonterm };
            return _tokens.Add(token) && _nonterms.Add(token);
        }

        public bool InsertProduction(string nonterm, List<Token> tokens) => _prodcs.Add(
                new Prodc
                {
                    _left = new Token
                    {
                        _type = Token.Type.NONTERMINAL,
                        _attr = nonterm
                    },
                    _right = tokens
                });

        public bool InsertProduction(Prodc prodc) => _prodcs.Add(prodc);

        public void SetStart(string nonterm) => _start = _nonterms.First(e => e._attr.Equals(nonterm)); /*new Token { _type = Token.Type.NONTERMINAL, _attr = nonterm };*/

        public Dictionary<Token, List<Token>> _fis = new Dictionary<Token, List<Token>>();
        public Dictionary<Token, List<Token>> _fos = new Dictionary<Token, List<Token>>();

        public void RunSpecificFIS(Token nonterm, List<Token> tokens)
        {
            List<Token> sfis = new List<Token>();
            Token fr = tokens.First();
            switch (fr._type)
            {
                case Token.Type.TERMINAL:
                    sfis.Add(fr);
                    break;
                case Token.Type.NONTERMINAL:
                    {
                        if (_fis.TryGetValue(fr, out sfis))
                        {
                            bool epsilon = false;
                            sfis.ForEach(e =>
                            {
                                if (e._attr.Equals('ε'))
                                {
                                    epsilon = true;
                                }
                            });
                            if (epsilon)
                            {
                                tokens.RemoveAt(0);
                                if (tokens.Count > 0)
                                {
                                    RunSpecificFIS(nonterm, tokens);
                                }
                            }
                        }
                    }
                    break;
                case Token.Type.INVALID:
                    break;
                default:
                    break;
            }
            _fis.Add(nonterm, sfis);
        }
    }
}
