using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;

namespace GrammarAnalyzer.Kernel
{
    abstract public class LRBaseGrammar : Grammar
    {
        static readonly protected string To = "→";
        static readonly protected string Dot = "·";
        protected struct Action
        {
            public enum Type
            {
                SHIFT,
                REDUC,
                ACC
            }
            public Type _type;
            public int _nextState;
            public Prodc _prodc;
            public Action(Type type)
            {
                _type = type;
                _nextState = -1;
                _prodc = new Prodc();
            }
            public Action(int nextState)
            {
                _type = Type.SHIFT;
                _nextState = nextState;
                _prodc = new Prodc();
            }
            public Action(Prodc prodc)
            {
                _type = Type.REDUC;
                _nextState = -1;
                _prodc = new Prodc(prodc);
            }
            public Action(Action action)
            {
                _type = action._type;
                _nextState = action._nextState;
                _prodc = new Prodc(action._prodc);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj is Action action && _type == action._type)
                {
                    switch (_type)
                    {
                        case Type.SHIFT:
                            return _nextState == action._nextState;
                        case Type.REDUC:
                            return _prodc == action._prodc;
                        case Type.ACC:
                            return true;
                    }
                }
                return false;
            }
            public override int GetHashCode() => _type switch
            {
                Type.SHIFT => _type.GetHashCode() * 17 + _nextState.GetHashCode(),
                Type.REDUC => _type.GetHashCode() * 17 + _prodc.GetHashCode(),
                Type.ACC => _type.GetHashCode(),
                _ => 0,
            };
            static public bool operator ==(Action a1, Action a2)
            {
                if (a1._type == a2._type)
                {
                    switch (a1._type)
                    {
                        case Type.SHIFT:
                            return a1._nextState == a2._nextState;
                        case Type.REDUC:
                            return a1._prodc == a2._prodc;
                        case Type.ACC:
                            return true;
                    }
                }
                return false;
            }
            static public bool operator !=(Action a1, Action a2)
            {
                if (a1._type == a2._type)
                {
                    switch (a1._type)
                    {
                        case Type.SHIFT:
                            return a1._nextState != a2._nextState;
                        case Type.REDUC:
                            return a1._prodc != a2._prodc;
                        case Type.ACC:
                            return false;
                    }
                }
                return true;
            }
        }
        protected Dictionary<ValueTuple<int, Token>, List<Action>> _acts = new Dictionary<(int, Token), List<Action>>();
        protected Dictionary<ValueTuple<int, Token>, int> _goto = new Dictionary<(int, Token), int>();

        protected class DerivBase
        {
            public Prodc _prodc;
            public int _point;
            public DerivBase(Prodc prodc, int point)
            {
                _prodc = new Prodc(prodc);
                _point = point;
            }
            public DerivBase(DerivBase deriv)
            {
                _prodc = new Prodc(deriv._prodc);
                _point = deriv._point;
            }
            static public bool operator ==(DerivBase d1, DerivBase d2)
            {
                if (d1 is null || d2 is null) return false;
                return d1._point == d2._point && d1._prodc == d2._prodc;
            }
            static public bool operator !=(DerivBase d1, DerivBase d2)
            {
                if (d1 is null || d2 is null) return false;
                return !(d1._point == d2._point && d1._prodc == d2._prodc);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is DerivBase deriv && _point == deriv._point && _prodc == deriv._prodc;
            }
            public override int GetHashCode()
            {
                return _prodc.GetHashCode() * 17 + _point;
            }
        }

        protected class StateBase
        {
            public int _id;
            public readonly HashSet<Prodc> _prodcs;
            public StateBase(int id, HashSet<Prodc> prodcs)
            {
                _id = id;
                _prodcs = prodcs;
            }
            virtual protected void Extend() { }
        }
        protected class DFABase
        {
            public Dictionary<ValueTuple<int, Token>, int> _trfs;
            public DFABase() => _trfs = new Dictionary<(int, Token), int>();
        }
        protected int _dfaStateCount = 0;
    }

    public class SLR : LRBaseGrammar
    {
        private class Deriv : DerivBase
        {
            public Deriv(Prodc prodc, int point) : base(prodc, point)
            {
            }
            public Deriv(Deriv deriv) : base(deriv._prodc, deriv._point)
            {
            }
        }
        private class State : StateBase
        {
            public HashSet<Deriv> _derivs = new HashSet<Deriv>();

            public State(int id, HashSet<Prodc> prodcs, HashSet<Deriv> derivs) : base(id, prodcs)
            {
                _derivs = new HashSet<Deriv>(derivs);
                Extend();
            }

            override protected void Extend()
            {
                Queue<Deriv> unhandled = new Queue<Deriv>(_derivs);
                while (unhandled.Count > 0)
                {
                    Deriv deriv = unhandled.Dequeue();
                    if (deriv._point < deriv._prodc._right.Count
                        && deriv._prodc._right[deriv._point]._type == Token.Type.NONTERMINAL)
                    {
                        foreach (var prodc in _prodcs)
                        {
                            if (prodc._left == deriv._prodc._right[deriv._point])
                            {
                                var added = new Deriv(prodc, 0);
                                _derivs.Add(added);
                                unhandled.Enqueue(added);
                            }
                        }
                    }
                }
            }
            static public bool operator ==(State s1, State s2)
            {
                if (s1 is null || s2 is null) return false;
                if (s1._derivs.Count != s2._derivs.Count) return false;
                foreach (var d in s1._derivs)
                {
                    if (!s2._derivs.Contains(d)) return false;
                }
                return true;
            }
            static public bool operator !=(State s1, State s2)
            {
                return !(s1 == s2);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is State state && this == state;
            }
            public override int GetHashCode()
            {
                return _derivs.GetHashCode();
            }
        }
        private class DFA : DFABase
        {
            public HashSet<State> _states;
            public DFA(State start) : base()
            {
                _states = new HashSet<State> { start };
            }
        }
        private DFA BuildDFA()
        {
            try
            {
                Prodc start = _prodcs.First(e => e._left == _start);
                DFA dfa = new DFA(new State(0, _prodcs, new HashSet<Deriv>() { new Deriv(start, 0) }));

                Queue<State> unhandled = new Queue<State>();
                unhandled.Enqueue(dfa._states.First());
                while (unhandled.Count > 0)
                {
                    State cur = unhandled.Dequeue();
                    foreach (var token in _tokens)
                    {
                        HashSet<Deriv> derivs = new HashSet<Deriv>();
                        foreach (var deriv in cur._derivs)
                        {
                            if (deriv._point < deriv._prodc._right.Count
                                && deriv._prodc._right[deriv._point] == token)
                            {
                                derivs.Add(new Deriv(deriv._prodc, deriv._point + 1));
                            }
                        }
                        if (derivs.Count == 0) continue;

                        State state = dfa._states.First(e => e._derivs == derivs);
                        if (state is null)
                        {
                            state = new State(dfa._states.Count, _prodcs, derivs);
                            unhandled.Enqueue(state);
                            if (dfa._trfs.ContainsKey((cur._id, token)))
                            {
                                dfa._trfs[(cur._id, token)] = dfa._states.Count;
                            }
                            else
                            {
                                dfa._trfs.Add((cur._id, token), dfa._states.Count);
                            }
                            dfa._states.Add(state);
                        }
                        else
                        {
                            if (dfa._trfs.ContainsKey((cur._id, token)))
                            {
                                dfa._trfs[(cur._id, token)] = state._id;
                            }
                            else
                            {
                                dfa._trfs.Add((cur._id, token), state._id);
                            }
                        }
                    }
                }

                return dfa;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Dictionary<int, List<string>> BuildDerivs()
        {
            DFA dfa = BuildDFA();
            _dfaStateCount = dfa._states.Count;

            Dictionary<int, List<string>> res = new Dictionary<int, List<string>>();
            foreach (var state in dfa._states)
            {
                List<string> vs = new List<string>();
                foreach (var deriv in state._derivs)
                {
                    string desc = deriv._prodc._left._attr + To;
                    for (int pos = 0; pos < deriv._prodc._right.Count; ++pos)
                    {
                        desc += ((pos == deriv._point) ? Dot : "") 
                            + ((deriv._prodc._right[pos] == Epsilon) ? "" : deriv._prodc._right[pos]._attr);
                    }
                    desc += (deriv._point == deriv._prodc._right.Count) ? Dot : "";
                    vs.Add(desc);
                }
                res.Add(state._id, vs);
            }

            foreach (var tr in dfa._trfs)
            {
                if (tr.Key.Item2._type == Token.Type.TERMINAL)
                {
                    _acts.Add(tr.Key, new List<Action> { new Action(tr.Value) });
                }
                else
                {
                    _goto.Add(tr.Key, tr.Value);
                }
            }

            foreach (var state in dfa._states)
            {
                foreach (var deriv in state._derivs)
                {
                    if (deriv._prodc._left == _start
                        && deriv._point == deriv._prodc._right.Count)
                    {
                        _acts.Add((state._id, Dollar), new List<Action> { new Action(Action.Type.ACC) });
                    }
                    else if (deriv._point == deriv._prodc._right.Count
                        || deriv._prodc._right.First() == Epsilon)
                    {
                        _fos.TryGetValue(deriv._prodc._left, out HashSet<Token> fos);
                        if (!(fos is null))
                        {
                            foreach (var token in fos)
                            {
                                _acts.TryGetValue((state._id, token), out List<Action> action);
                                if (action is null)
                                {
                                    _acts.Add((state._id, token), new List<Action> { new Action(deriv._prodc) });
                                }
                                else
                                {
                                    action.Add(new Action(deriv._prodc));
                                    _acts[(state._id, token)] = action;
                                }
                            }
                        }
                    }
                }
            }
            return res;
        }
    }

    public class LR : LRBaseGrammar
    {
        private class Deriv : DerivBase
        {
            public Token _tail;
            public Deriv(Prodc prodc, int point, Token tail) : base(prodc, point)
            {
                _tail = new Token(tail);
            }
            public Deriv(Deriv deriv) : base(deriv._prodc, deriv._point)
            {
                _tail = new Token(deriv._tail);
            }
            static public bool operator ==(Deriv d1, Deriv d2)
            {
                if (d1 is null || d2 is null) return false;
                return d1._point == d2._point && d1._tail == d2._tail && d1._prodc == d2._prodc;
            }
            static public bool operator !=(Deriv d1, Deriv d2)
            {
                if (d1 is null || d2 is null) return false;
                return !(d1._point == d2._point && d1._tail == d2._tail && d1._prodc == d2._prodc);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Deriv deriv && this == deriv;
            }
            public override int GetHashCode()
            {
                return _tail.GetHashCode() * 17 + base.GetHashCode();
            }
        }
        private class State : StateBase
        {
            public HashSet<Deriv> _derivs = new HashSet<Deriv>();
            public readonly LR _lr = null;

            public State(int id, HashSet<Prodc> prodcs, HashSet<Deriv> derivs, LR lr) : base(id, prodcs)
            {
                _derivs = new HashSet<Deriv>(derivs);
                _lr = lr;
                Extend();
            }

            override protected void Extend()
            {
                Queue<Deriv> unhandled = new Queue<Deriv>(_derivs);
                while (unhandled.Count > 0)
                {
                    Deriv deriv = unhandled.Dequeue();
                    if (deriv._point < deriv._prodc._right.Count
                        && deriv._prodc._right[deriv._point]._type == Token.Type.NONTERMINAL)
                    {
                        List<Token> can = (deriv._point + 1 < deriv._prodc._right.Count) ?
                            deriv._prodc._right.Skip(deriv._point + 1).ToList() : new List<Token>();
                        can.Add(deriv._tail);

                        HashSet<Token> fis = _lr.SubFIS(can);
                        foreach (var prodc in _prodcs)
                        {
                            foreach (var token in fis)
                            {
                                if (prodc._left == deriv._prodc._right[deriv._point])
                                {
                                    var added = new Deriv(prodc, 0, token);
                                    _derivs.Add(added);
                                    unhandled.Enqueue(added);
                                }
                            }
                        }
                    }
                }
            }
            static public bool operator ==(State s1, State s2)
            {
                if (s1 is null || s2 is null) return false;
                if (s1._derivs.Count != s2._derivs.Count) return false;
                foreach (var d in s1._derivs)
                {
                    if (!s2._derivs.Contains(d)) return false;
                }
                return true;
            }
            static public bool operator !=(State s1, State s2)
            {
                return !(s1 == s2);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is State state && this == state;
            }
            public override int GetHashCode()
            {
                return _derivs.GetHashCode();
            }
        }
        private class DFA : DFABase
        {
            public List<State> _states;
            public DFA(State start) : base()
            {
                _states = new List<State> { start };
            }
        }
        //private DFA BuildDFA()
        //{
        //    DFA dfa = new DFA(new State());
        //    return dfa;
        //}
    }
}
