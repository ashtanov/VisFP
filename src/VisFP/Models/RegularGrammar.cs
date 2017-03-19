using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    [JsonObject]
    public class RegularGrammar
    {
        public IReadOnlyList<Rule> Rules { get; private set; }
        public Alphabet Alph { get; private set; }
        public RegularGrammar(Alphabet alph, IReadOnlyList<Rule> rules)
        {
            Rules = rules;
            Alph = alph;
        }

        [JsonIgnore]
        private char[] _reachableNonTerminals;
        [JsonIgnore]
        private char[] _generatingNonTerminals;
        [JsonIgnore]
        public char[] ReachableNonterminals
        {
            get
            {
                if (_reachableNonTerminals == null)
                    FindReachableNonTerminals();
                return _reachableNonTerminals;
            }
        }
        [JsonIgnore]
        public char[] GeneratingNonterminals
        {
            get
            {
                if (_generatingNonTerminals == null)
                    FindGeneratingNonTerminals();
                return _generatingNonTerminals;
            }
        }

        private void FindReachableNonTerminals()
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

        private void FindGeneratingNonTerminals()
        {
            HashSet<char> generating = 
                new HashSet<char>(Rules
                    .Where(x => !x.Rnt.HasValue)
                    .Select(x => x.Lnt)
                    .Distinct());
            var prevSetLength = 0;
            while(prevSetLength != generating.Count)
            {
                prevSetLength = generating.Count;
                foreach(var r in Rules.Where(x => x.Rnt.HasValue && generating.Contains(x.Rnt.Value)))
                    generating.Add(r.Lnt);
            }
            _generatingNonTerminals = generating.ToArray();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static RegularGrammar Parse(string jsonObject)
        {
            JObject parsed = JsonConvert.DeserializeObject<JObject>(jsonObject);
            var alphabet = new Alphabet(
                init: parsed[nameof(Alph)]["InitState"].Value<char>(),
                term: parsed[nameof(Alph)]["Terminals"].ToObject<char[]>(),
                notTerm: parsed[nameof(Alph)]["NonTerminals"].ToObject<char[]>());
            var rules = parsed[nameof(Rules)].ToObject<Rule[]>();
            return new RegularGrammar(alphabet, rules);
        }
    }
}
