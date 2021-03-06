﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.BusinessObjects
{
    [JsonObject]
    public class Alphabet
    {
        public readonly IReadOnlyList<char> Terminals;
        public readonly IReadOnlyList<char> NonTerminals;
        public readonly char InitState;
        public readonly char FiniteState;


        public Alphabet(char init, char[] term, char[] notTerm)
        {
            var _terminals = new char[term.Length];
            var _notTerminals = new char[notTerm.Length];

            term.CopyTo(_terminals, 0);
            notTerm.CopyTo(_notTerminals, 0);

            Terminals = new List<char>(_terminals);
            NonTerminals = new List<char>(_notTerminals);
            InitState = init;
            FiniteState = '$';
            if (!_notTerminals.Contains(init))
                throw new ArgumentException($"Начальный символ {init} должен содержаться в множестве нетерминалов!");
        }

        public Alphabet(char init, char[] term, char[] notTerm, char finite)
            : this(init, term, notTerm)
        {
            FiniteState = finite;
            if (!notTerm.Contains(finite))
                throw new ArgumentException($"Финальный символ {FiniteState} должен содержаться в множестве нетерминалов!");

        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Alphabet Deserialize(string jsonObject)
        {
            return null;
        }

        public static Alphabet GenerateRandomRg(int nonTermCount, int termCount)
        {
            Random r = new Random();
            char initSymbol = 'S';
            string nt = "TUVWXYZ";
            string t = "1234567890abcdef";
            return new Alphabet(
                initSymbol,
                t.OrderBy(x => r.Next()).Take(termCount).ToArray(),
                new[] { initSymbol }.Union(nt.OrderBy(x => r.Next()).Take(nonTermCount - 1)).ToArray());
        }

        public static Alphabet GenerateRandomFsm(int nonTermCount, int termCount)
        {
            Random r = new Random();
            char initSymbol = 'S';
            string nt = "TUVWXYZ";
            string t = "1234567890abcdef";
            var notterm = nt.OrderBy(x => r.Next()).Take(nonTermCount - 1).ToList();
            return new Alphabet(
                initSymbol,
                t.OrderBy(x => r.Next()).Take(termCount).ToArray(),
                new[] { initSymbol }.Union(notterm).ToArray(),
                notterm.OrderBy(x => r.Next()).First()
                );
        }
    }

    public class Generator
    {
        static Generator _instance;
        static object _locker = new object();
        Random _rand;
        private Generator()
        {
            _rand = new Random();
        }
        public static Generator Instance
        {
            get
            {
                if (_instance == null)
                    lock (_locker)
                        if (_instance == null)
                            _instance = new Generator();
                return _instance;
            }
        }

        public RegularGrammar GenerateRg(int ntRuleCount, int tRuleCount, Alphabet alph)
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
                if (!result.Contains(curr))
                    result.Add(curr);
            }
            for (int i = 0; i < tRuleCount; ++i)
            {
                var curr = new Rule(
                          Lnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)],
                          Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                          Rnt: alph.FiniteState,
                          isFinite: true
                      );
                if (!result.Contains(curr))
                    result.Add(curr);
            }
            result = result.OrderBy(x => x.Lnt).ToList();
            return new RegularGrammar(alph, result);
        }

        public RegularGrammar GenerateFsm(int ntRuleCount, int tRuleCount, Alphabet alph)
        {
            var result = new List<Rule>();
            var initRuleEndPoint = alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)];
            result.Add(
                    new Rule(
                        Lnt: alph.InitState,
                        Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                        Rnt: initRuleEndPoint,
                        isFinite: initRuleEndPoint == alph.FiniteState
                    ));
            var notfinite = alph.NonTerminals.Except(new[] { alph.FiniteState }).ToList();
            for (int i = 1; i < ntRuleCount; ++i)
            {
                var curr = new Rule(
                        Lnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)],
                        Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                        Rnt: notfinite[_rand.Next(notfinite.Count)]
                    );
                if (!result.Contains(curr))
                    result.Add(curr);
            }
            for (int i = 0; i < tRuleCount; ++i)
            {
                var curr = new Rule(
                          Lnt: alph.NonTerminals[_rand.Next(alph.NonTerminals.Count)],
                          Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                          Rnt: alph.FiniteState,
                          isFinite: true
                      );
                if (!result.Contains(curr))
                    result.Add(curr);
            }
            result = result.OrderBy(x => x.Lnt).ToList();
            return new RegularGrammar(alph, result);
        }
    }
}
