using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;

namespace GrammarAnalyzer.Kernel
{
    public class LRGrammar : Grammar
    {
        public struct Action
        {
            public enum Type
            {
                SHIFT,
                REDUC,
                ACC
            }
            Type _type;
            int _nextState;
            Prodc _prodc;
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
        private Dictionary<ValueTuple<int, Token>, List<Action>> _acs = new Dictionary<(int, Token), List<Action>>();
        private Dictionary<ValueTuple<int, Token>, int> _goto = new Dictionary<(int, Token), int>();

        private struct SLRDeriv
        {
            public Prodc _prodc;
            public int _point;
            public SLRDeriv(Prodc prodc, int point)
            {
                _prodc = new Prodc(prodc);
                _point = point;
            }
            public SLRDeriv(SLRDeriv deriv)
            {
                _prodc = new Prodc(deriv._prodc);
                _point = deriv._point;
            }
            static public bool operator ==(SLRDeriv d1, SLRDeriv d2)
            {
                return d1._point == d2._point && d1._prodc == d2._prodc;
            }
            static public bool operator !=(SLRDeriv d1, SLRDeriv d2)
            {
                return !(d1._point == d2._point && d1._prodc == d2._prodc);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is SLRDeriv deriv && _point == deriv._point && _prodc == deriv._prodc;
            }
            public override int GetHashCode()
            {
                return _prodc.GetHashCode() * 17 + _point;
            }
        }

        private struct SLRState
        {
            public int _id;
            public HashSet<SLRDeriv> _derivs;
            public readonly HashSet<Prodc> _prodcs;
            public SLRState(int id, HashSet<SLRDeriv> derivs, HashSet<Prodc> prodcs)
            {
                _id = id;
                _derivs = derivs;
                _prodcs = prodcs;
                Extend();
            }
            private void Extend()
            {
                Queue<SLRDeriv> unhandled = new Queue<SLRDeriv>(_derivs);
                while (unhandled.Count > 0)
                {
                    SLRDeriv deriv = unhandled.Dequeue();
                    if (deriv._point < deriv._prodc._right.Count
                        && deriv._prodc._right[deriv._point]._type == Token.Type.NONTERMINAL)
                    {
                        foreach (var prodc in _prodcs)
                        {
                            SLRDeriv added = new SLRDeriv(prodc, 0);
                            if (prodc._left == deriv._prodc._right[deriv._point])
                            {
                                _derivs.Add(added);
                                unhandled.Enqueue(added);
                            }
                        }
                    }
                }
            }
            static public bool operator ==(SLRState s1, SLRState s2)
            {
                if (s1._derivs.Count != s2._derivs.Count) return false;
                foreach (var d in s1._derivs)
                {
                    if (!s2._derivs.Contains(d)) return false;
                }
                return true;
            }
            static public bool operator !=(SLRState s1, SLRState s2)
            {
                return !(s1 == s2);
            }
            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is SLRState state && this == state;
            }
            public override int GetHashCode()
            {
                return _derivs.GetHashCode();
            }
        }
        private class SLRDFA
        {
            public List<SLRState> _states;
            public Dictionary<ValueTuple<int, Token>, int> _trfs;

            public SLRDFA(SLRState start)
            {
                _states = new List<SLRState> { start };
                _trfs = new Dictionary<(int, Token), int>();
            }
        }
        private int _dfaStateCount = 0;
        private SLRDFA BuildSLRDFA()
        {
            SLRDFA dfa = new SLRDFA(new SLRState());
            return dfa;
        }
    }
}
