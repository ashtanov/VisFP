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
        public char[] ReachableNonterminals
        {
            get
            {
                if (_reachableNonTerminals == null)
                    FindReachableNonTerminals();
                return _reachableNonTerminals;
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

        [JsonIgnore]
        private char[] _generatingNonTerminals;
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
        private void FindGeneratingNonTerminals()
        {
            HashSet<char> generating =
                new HashSet<char>(Rules
                    .Where(x => !x.Rnt.HasValue)
                    .Select(x => x.Lnt)
                    .Distinct());
            var prevSetLength = 0;
            while (prevSetLength != generating.Count)
            {
                prevSetLength = generating.Count;
                foreach (var r in Rules.Where(x => x.Rnt.HasValue && generating.Contains(x.Rnt.Value)))
                    generating.Add(r.Lnt);
            }
            _generatingNonTerminals = generating.ToArray();
        }

        [JsonIgnore]
        private char[] _cyclicNonTerminals;
        [JsonIgnore]
        public char[] CyclicNonterminals
        {
            get
            {
                if (_cyclicNonTerminals == null)
                    FindCyclicNonTerminals();
                return _cyclicNonTerminals;
            }
        }
        private void FindCyclicNonTerminals()
        {
            List<char> cyclic = new List<char>();
            foreach (var r in Rules)
                if (r.Rnt.HasValue && r.Rnt.Value == r.Lnt)
                    cyclic.Add(r.Lnt);
            _cyclicNonTerminals = cyclic.Distinct().ToArray();
        }

        public bool IsProper //приведенная ли?
        {
            get
            {
                return 
                    ReachableNonterminals.Length == Alph.NonTerminals.Count 
                    && GeneratingNonterminals.Length == Alph.NonTerminals.Count;
            }
        }

        public bool IsEmptyLanguage //порождает ли пустой язык?
        {
            get
            {
                return !GeneratingNonterminals.Contains(Alph.InitState);
            }
        }

        /// <summary>
        /// Получить приведенную версию грамматики
        /// </summary>
        /// <returns>Если null - значит исходную привести нельзя!</returns>
        public RegularGrammar GetProperVersion()
        {
            try
            {
                //удаление "бесплодных" символов
                var emptySymbols = new HashSet<char>(Alph.NonTerminals.Except(GeneratingNonterminals));
                var newRules = Rules
                    .Where(x => !emptySymbols.Contains(x.Lnt) || (x.Rnt.HasValue && !emptySymbols.Contains(x.Rnt.Value))).ToList();
                RegularGrammar tmp = new RegularGrammar(
                    new Alphabet(
                        Alph.InitState,
                        Alph.Terminals.ToArray(),
                        Alph.NonTerminals.Except(emptySymbols).ToArray()
                        ),
                    newRules);
                //из получившейся грамматики удаляем недостижимые символы
                var unreachableSymbols = new HashSet<char>(tmp.Alph.NonTerminals.Except(tmp.ReachableNonterminals));
                var newRules2 = tmp.Rules
                    .Where(x => !unreachableSymbols.Contains(x.Lnt) || (x.Rnt.HasValue && !unreachableSymbols.Contains(x.Rnt.Value))).ToList();
                return
                    new RegularGrammar(new Alphabet(
                        tmp.Alph.InitState,
                        tmp.Alph.Terminals.ToArray(),
                        tmp.Alph.NonTerminals.Except(unreachableSymbols).ToArray()
                        ),
                    newRules2);
            }
            catch(ArgumentException ex)
            {
                return null;
            }
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
