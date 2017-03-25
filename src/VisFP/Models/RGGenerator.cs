using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models
{
    [JsonObject]
    public class Alphabet
    {
        public readonly IReadOnlyList<char> Terminals;
        public readonly IReadOnlyList<char> NonTerminals;
        public readonly char InitState;

        public Alphabet(char init, char[] term, char[] notTerm)
        {
            var _terminals = new char[term.Length];
            var _notTerminals = new char[notTerm.Length];

            term.CopyTo(_terminals, 0);
            notTerm.CopyTo(_notTerminals, 0);

            Terminals = new List<char>(_terminals);
            NonTerminals = new List<char>(_notTerminals);
            InitState = init;
            if (!_notTerminals.Contains(init))
                throw new ArgumentException($"Начальный символ {init} должен содержаться в множестве нетерминалов!");
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Alphabet Deserialize(string jsonObject)
        {
            return null;
        }

        public static Alphabet GenerateRandom(int nonTermCount, int termCount)
        {
            Random r = new Random();
            char initSymbol = 'S';
            string nt = "TUVWXYZ";
            string t = "1234567890abcdef";
            return new Alphabet(
                initSymbol,
                t.OrderBy(x => r.Next()).Take(termCount).ToArray(),
                new[] { initSymbol }.Union(nt.OrderBy(x => r.Next()).Take(nonTermCount-1)).ToArray());
        }
    }

    public class RGGenerator
    {
        static RGGenerator _instance;
        static object _locker = new object();
        Random _rand;
        private RGGenerator()
        {
            _rand = new Random();
        }
        public static RGGenerator Instance
        {
            get
            {
                if (_instance == null)
                    lock (_locker)
                        if (_instance == null)
                            _instance = new RGGenerator();
                return _instance;
            }
        }

        public RegularGrammar Generate(int ntRuleCount, int tRuleCount, Alphabet alph)
        {
            var result = new List<Rule>();
            result.Add(
                    new Rule(
                        Lnt: alph.InitState,
                        Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                        Rnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)]
                    ));
            for (int i = 1; i < ntRuleCount; ++i)
            {
                var curr = new Rule(
                        Lnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)],
                        Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                        Rnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)]
                    );
                if(!result.Contains(curr))
                    result.Add(curr);
            }
            for (int i = 0; i < tRuleCount; ++i)
            {
                var curr = new Rule(
                          Lnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)],
                          Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                          Rnt: null
                      );
                if (!result.Contains(curr))
                    result.Add(curr);
            }
            result = result.OrderBy(x => x.Lnt).ToList();
            return new RegularGrammar(alph, result);
        }


    }
}
