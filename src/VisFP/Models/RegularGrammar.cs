using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models
{
    public class Rule : IEquatable<Rule>
    {
        public readonly char Lnt; //левая часть правила - нетреминал
        public readonly char Rt; //правая часть правила - терминал
        public readonly char? Rnt; //правая часть правила - нетерминал

        public Rule(char Lnt, char Rt, char? Rnt)
        {
            this.Lnt = Lnt;
            this.Rnt = Rnt;
            this.Rt = Rt;
        }

        public override string ToString()
        {
            if (Rnt.HasValue)
                return $"{Lnt} -> {Rt}{Rnt}";
            else
                return $"{Lnt} -> {Rt}";
        }

        public bool Equals(Rule other)
        {
            return Lnt == other.Lnt && Rt == other.Rt && Rnt == other.Rnt;
        }
    }

    public class RegularGrammar
    {
        private char[] _reachableNonTerminals;

        public IReadOnlyList<Rule> Rules { get; private set; }
        public Alphabet Alph { get; private set; }
        public RegularGrammar(Alphabet alph, IReadOnlyList<Rule> rules)
        {
            Rules = rules;
            Alph = alph;
        }

        public char[] ReachableNonterminals
        {
            get
            {
                if (_reachableNonTerminals == null)
                    FindReachableNonTerminals();
                return _reachableNonTerminals;
            }
        }

        void FindReachableNonTerminals()
        {
            HashSet<char> reachable = new HashSet<char>();
            reachable.Add(Alph.InitState);
            Queue<char> q = new Queue<char>();
            q.Enqueue(Alph.InitState);
            while (q.Count != 0)
            {
                var current = q.Dequeue();
                foreach (var nt in Rules.Where(x => x.Lnt == current))
                {
                    if (nt.Rnt.HasValue && !reachable.Contains(nt.Rnt.Value))
                    {
                        reachable.Add(nt.Rnt.Value);
                        q.Enqueue(nt.Rnt.Value);
                    }
                }
            }
            _reachableNonTerminals = reachable.ToArray();
        }
    }
}
