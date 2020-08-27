using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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
        protected static Token Dollar = new Token(Token.Type.TERMINAL, "＄");

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
        public void SetStart(Token token)
            => _start = new Token(_nonterms.First(e => e == token));

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
        protected void Extend()
        {
            Token dot = new Token(_start);
            do
            {
                dot._attr += '\'';
            } while (_tokens.Contains(dot));
            _nonterms.Add(dot); _tokens.Add(dot);

            _prodcs.Add(new Prodc(dot, new List<Token> { _start }));
            _start = new Token(dot);
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
            foreach (var u in _prodcs)
            {
                u._right.ForEach(v =>
                {
                    _connect.Add(new TokenPair(u._left, v));
                    if (v == Epsilon)
                    {
                        _terms.Add(Epsilon);
                        _tokens.Add(Epsilon);
                    }
                });
            }
            Queue<Token> oris = new Queue<Token>();
            oris.Enqueue(_tokens.First());
            
            while (oris.Count > 0)
            {
                Token cur = oris.Dequeue();
                _checked.Add(cur);
                foreach (var e in _connect)
                {
                    Token adj = e.Contains(cur);
                    if (adj != cur && !_checked.Contains(adj))
                    {
                        oris.Enqueue(adj);
                    }
                }
            }
            return _checked.Count == _tokens.Count;
        }
        /// <summary>
        /// check every sub-candidates
        /// </summary>
        /// <param name="can"></param>
        /// <returns></returns>
        protected HashSet<Token> SubFIS(List<Token> can)
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
                            HashSet<Token> more = SubFIS(can.Skip(1).ToList());
                            if (more is null)
                            {
                                return fis;
                            }
                            return fis.Concat(more).ToHashSet();
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
        public Dictionary<Token, HashSet<Token>> RunFIS()
        {
            _fis = new Dictionary<Token, HashSet<Token>>();
            foreach (var e in _prodcs)
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
            }

            int fc = 0;
            do
            {
                fc = 0;
                // first pass
                foreach (var e in _prodcs)
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
                                _fis[e._left] = fis.Concat(more).ToHashSet();
                            }
                        }
                    }
                }
                foreach (var item in _fis)
                {
                    fc += item.Value.Count;
                }

                // second pass
                foreach (var e in _prodcs)
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
                                _fis[e._left] = fis.Concat(more).ToHashSet();
                            }
                        }
                    }
                }
                foreach (var item in _fis)
                {
                    fc -= item.Value.Count;
                }
            } while (fc != 0);
            if (_fis.Count != _nonterms.Count) _fis.Clear();
            return _fis.Count == _nonterms.Count ? new Dictionary<Token, HashSet<Token>>(_fis) : null;
        }
        private HashSet<Token> SubFOS(Token dest, List<Token> sours, List<Token> done)
        {
            HashSet<Token> fos = new HashSet<Token>();
            foreach (var prodc in _prodcs)
            {
                List<Token> can = prodc._right;
                
                for (int i = 0; i < can.Count; ++i)
                {
                    if (can[i] == dest)
                    {
                        bool epsilon = false;
                        if (++i < can.Count)
                        {
                            HashSet<Token> more = SubFIS(can.Skip(i).ToList());
                            if (!(more is null))
                            {
                                foreach (var token in more)
                                {
                                    if (token == Epsilon)
                                    {
                                        epsilon = true;
                                    }
                                    else
                                    {
                                        fos.Add(token);
                                    }
                                }
                            }
                        }
                        else
                        {
                            epsilon = true;
                        }
                        if (epsilon)
                        {
                            if (done.Contains(prodc._left))
                            {
                                _fos.TryGetValue(prodc._left, out HashSet<Token> more);
                                if (!(more is null))
                                {
                                    fos = fos.Concat(more).ToHashSet();
                                }
                            }
                            else if (prodc._left != sours.First()
                                && prodc._left != dest)
                            {
                                if (!sours.Skip(1).Contains(prodc._left))
                                {
                                    sours.Add(prodc._left);
                                    HashSet<Token> more = SubFOS(prodc._left, sours, done);
                                    if (sours.Count == 0)
                                    {
                                        return null;
                                    }
                                    else
                                    {
                                        sours.Remove(sours.Last());
                                        if (prodc._left == _start)
                                        {
                                            fos.Add(Dollar);
                                        }
                                        if (!(more is null))
                                        {
                                            fos = fos.Concat(more).ToHashSet();
                                        }
                                    }
                                }
                                else
                                {
                                    sours.Clear();
                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            return fos;
        }
        public Dictionary<Token, HashSet<Token>> RunFOS()
        {
            _fos = new Dictionary<Token, HashSet<Token>>
            {
                { _start, new HashSet<Token> { Dollar } }
            };

            List<Token> done = new List<Token>();
            int fc;
            do
            {
                fc = 0;
                // first pass
                foreach (var token in _nonterms)
                {
                    if (!done.Contains(token))
                    {
                        List<Token> sours = new List<Token> { token };
                        HashSet<Token> more = SubFOS(token, sours, done);
                        if (!(more is null))
                        {
                            _fos.TryGetValue(token, out HashSet<Token> fos);
                            if (fos is null)
                            {
                                _fos.Add(token, more);
                            }
                            else
                            {
                                _fos[token] = fos.Concat(more).ToHashSet();
                            }
                            done.Add(token);
                        }
                    }
                }
                foreach (var item in _fis)
                {
                    fc += item.Value.Count;
                }

                // second pass
                foreach (var token in _nonterms)
                {
                    if (!done.Contains(token))
                    {
                        List<Token> sours = new List<Token> { token };
                        HashSet<Token> more = SubFOS(token, sours, done);
                        if (!(more is null))
                        {
                            _fos.TryGetValue(token, out HashSet<Token> fos);
                            if (fos is null)
                            {
                                _fos.Add(token, more);
                            }
                            else
                            {
                                _fos[token] = fos.Concat(more).ToHashSet();
                            }
                            done.Add(token);
                        }
                    }
                }
                foreach (var item in _fis)
                {
                    fc -= item.Value.Count;
                }
            } while (fc != 0);
            if (done.Count != _nonterms.Count) _fos.Clear();
            return done.Count == _nonterms.Count ? new Dictionary<Token, HashSet<Token>>(_fos) : null;
        }
        public HashSet<Token> Tokens => new HashSet<Token>(_tokens);
        public HashSet<Token> Terms => new HashSet<Token>(_terms);
        public HashSet<Token> Nonterms => new HashSet<Token>(_nonterms);
        public HashSet<Prodc> Prodcs => new HashSet<Prodc>(_prodcs);
        public Token Start => new Token(_start);
    }
}
