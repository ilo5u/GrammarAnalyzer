using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GrammarAnalyzer.Kernel
{
    public class Grammar
    {
        public struct Token
        {
            public enum Type
            {
                TERMINAL,
                NONTERMINAL,
                INVALID
            }
            public Type _type;
            public string _attr;
            public Token(Type type, string attr)
            {
                _type = type;
                _attr = attr;
            }
            public Token(Token token)
            {
                _type = token._type;
                _attr = token._attr;
            }
            static public bool operator==(Token t1, Token t2)
            {
                return t1._attr == t2._attr;
            }
            static public bool operator !=(Token t1, Token t2)
            {
                return t1._attr != t2._attr;
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Token token && string.Equals(_attr, token._attr, StringComparison.InvariantCulture);
            }
            public override int GetHashCode()
            {
                return _attr is null ? StringComparer.InvariantCulture.GetHashCode(_attr) : 0;
            }
        }

        protected static Token Epsilon = new Token(Token.Type.TERMINAL, "ε");

        public struct Prodc
        {
            public Token _left;
            public List<Token> _right;

            public Prodc(Token left, List<Token> right)
            {
                _left = left;
                _right = new List<Token>(right);
            }
            public Prodc(Prodc prodc)
            {
                _left = prodc._left;
                _right = new List<Token>(prodc._right);
            }
            static public bool operator==(Prodc p1, Prodc p2)
            {
                bool eq = p1._right.Count == p2._right.Count && p1._left == p2._left;
                if (eq)
                {
                    for (int i = 0; i < p1._right.Count; ++i)
                        eq &= p1._right[i] == p2._right[i];
                }
                return eq;
            }
            static public bool operator !=(Prodc p1, Prodc p2)
            {
                bool eq = p1._right.Count == p2._right.Count && p1._left == p2._left;
                if (eq)
                {
                    for (int i = 0; i < p1._right.Count; ++i)
                        eq &= p1._right[i] == p2._right[i];
                }
                return !eq;
            }
            private bool Equals(Prodc prodc)
            {
                bool eq = _right.Count == prodc._right.Count && _left == prodc._left;
                if (eq)
                {
                    for (int i = 0; i < _right.Count; ++i)
                        eq &= _right[i] == prodc._right[i];
                }
                return eq;
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Prodc prodc && Equals(prodc);
            }
            public override int GetHashCode()
            {
                return _right is null ? _left.GetHashCode() * 17 + _right.GetHashCode() : 0;
            }
        }

        protected HashSet<Token> _tokens = new HashSet<Token>();
        protected HashSet<Token> _terms = new HashSet<Token>();
        protected HashSet<Token> _nonterms = new HashSet<Token>();
        protected HashSet<Prodc> _prodcs = new HashSet<Prodc>();
        protected Token _start = new Token();

        public bool InsertTerminal(string term)
            => _tokens.Add(new Token { _type = Token.Type.TERMINAL, _attr = term })
                && _terms.Add(new Token { _type = Token.Type.TERMINAL, _attr = term });
        public bool InsertToken(Token token)
            => token._type == Token.Type.TERMINAL ?
                _tokens.Add(new Token(token)) && _terms.Add(new Token(token)) : _tokens.Add(new Token(token)) && _nonterms.Add(new Token(token));
        public bool InsertNonterminal(string nonterm)
            => _tokens.Add(new Token { _type = Token.Type.NONTERMINAL, _attr = nonterm })
                && _nonterms.Add(new Token { _type = Token.Type.NONTERMINAL, _attr = nonterm });

        public bool InsertProduction(Token token, List<Token> can)
            => _prodcs.Add(new Prodc(token, can));

        public bool InsertProduction(Prodc prodc)
            => _prodcs.Add(new Prodc(prodc));

        public void SetStart(string nonterm)
            => _start = new Token(_nonterms.First(e => string.Equals(e._attr, nonterm, StringComparison.InvariantCulture))); /*new Token { _type = Token.Type.NONTERMINAL, _attr = nonterm };*/

        protected Dictionary<Token, HashSet<Token>> _fis = new Dictionary<Token, HashSet<Token>>();
        protected Dictionary<Token, HashSet<Token>> _fos = new Dictionary<Token, HashSet<Token>>();

        protected struct TokenPair
        {
            public Token _t1;
            public Token _t2;
            public TokenPair(Token t1, Token t2)
            {
                _t1 = new Token(t1);
                _t2 = new Token(t2);
            }
            public TokenPair(TokenPair tp)
            {
                _t1 = new Token(tp._t1);
                _t2 = new Token(tp._t2);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is TokenPair tp && ((_t1 == tp._t1 && _t2 == tp._t2) || (_t1 == tp._t2 && _t2 == tp._t1));
            }
            public override int GetHashCode()
            {
                return _t1.GetHashCode() * 17 + _t2.GetHashCode();
            }
            static public bool operator==(TokenPair t1, TokenPair t2)
            {
                return (t1._t1 == t2._t1 && t1._t2 == t2._t2) || (t1._t1 == t2._t2 && t1._t2 == t2._t1);
            }
            static public bool operator !=(TokenPair t1, TokenPair t2)
            {
                return !((t1._t1 == t2._t1 && t1._t2 == t2._t2) || (t1._t1 == t2._t2 && t1._t2 == t2._t1));
            }
            public Token Contains(Token token)
            {
                return _t1 == token ? _t2 : (_t2 == token ? _t1 : token);
            }
        }

        protected HashSet<TokenPair> _connect = new HashSet<TokenPair>();
        protected HashSet<Token> _checked = new HashSet<Token>();
        /// <summary>
        /// Check unreachable tokens
        /// </summary>
        /// <returns></returns>
        public bool ConnectivityTest()
        {
            _connect = new HashSet<TokenPair>();
            _checked = new HashSet<Token>();
            _prodcs.ToList().ForEach(u => 
                u._right.ForEach(v => { 
                    _connect.Add(new TokenPair(u._left, v));
                    if (v == Epsilon)
                    {
                        _terms.Add(Epsilon);
                        _tokens.Add(Epsilon);
                    }
                })
            );
            Queue<Token> oris = new Queue<Token>();
            oris.Enqueue(_tokens.First());
            
            while (oris.Count > 0)
            {
                Token cur = oris.Dequeue();
                _checked.Add(cur);
                _connect.ToList().ForEach(e =>
                {
                    Token adj = e.Contains(cur);
                    if (adj != cur && !_checked.Contains(adj))
                    {
                        oris.Enqueue(adj);
                    }
                });
            }
            return _checked.Count == _tokens.Count;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="can"></param>
        /// <returns></returns>
        private HashSet<Token> SubFIS(List<Token> can)
        {
            Token fr = can.First();
            switch (fr._type)
            {
                case Token.Type.TERMINAL:
                    return new HashSet<Token> { fr };
                case Token.Type.NONTERMINAL:
                    _fis.TryGetValue(fr, out HashSet<Token> fis);
                    if (fis is null)
                    {
                        return null;
                    }
                    else
                    {
                        if (can.Count != 1 && fis.Contains(Epsilon))
                        {
                            return fis.Concat(SubFIS(can.Skip(1).ToList())).ToHashSet();
                        }
                        else
                        {
                            return fis;
                        }
                    }
                case Token.Type.INVALID:
                    break;
                default:
                    break;
            }
            return null;
        }
        /// <summary>
        /// You'd better run this after left-recursion and common-tokens dismissed with extened LL(1) syntax
        /// </summary>
        /// <param name="nonterm"></param>
        /// <param name="tokens"></param>
        public bool RunFIS()
        {
            _fis = new Dictionary<Token, HashSet<Token>>();
            _prodcs.ToList().ForEach(e =>
            {
                Token fr = e._right.First();
                if (fr._type == Token.Type.TERMINAL)
                {
                    _fis.TryGetValue(e._left, out HashSet<Token> fis);
                    if (fis is null)
                    {
                        _fis.Add(e._left, new HashSet<Token> { new Token(fr) });
                    }
                    else
                    {
                        fis.Add(new Token(fr));
                    }
                }
            });

            int fc = 0;
            do
            {
                // first pass
                _prodcs.ToList().ForEach(e =>
                {
                    Token fr = e._right.First();
                    if (fr._type == Token.Type.NONTERMINAL)
                    {
                        HashSet<Token> more = SubFIS(e._right);
                        if (!(more is null))
                        {
                            _fis.TryGetValue(e._left, out HashSet<Token> fis);
                            if (fis is null)
                            {
                                _fis.Add(e._left, more);
                            }
                            else
                            {
                                fis = fis.Concat(more).ToHashSet();
                            }
                        }
                    }
                });
                _fis.ToList().ForEach(e => fc += e.Value.Count);

                // second pass
                _prodcs.ToList().ForEach(e =>
                {
                    Token fr = e._right.First();
                    if (fr._type == Token.Type.NONTERMINAL)
                    {
                        HashSet<Token> more = SubFIS(e._right);
                        if (!(more is null))
                        {
                            _fis.TryGetValue(e._left, out HashSet<Token> fis);
                            if (fis is null)
                            {
                                _fis.Add(e._left, more);
                            }
                            else
                            {
                                fis = fis.Concat(more).ToHashSet();
                            }
                        }
                    }
                });
                _fis.ToList().ForEach(e => fc -= e.Value.Count);
            } while (fc != 0);

            return _fis.Count == _nonterms.Count;
        }
    }
}
