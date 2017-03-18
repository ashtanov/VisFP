﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.Models
{
    public class Alphabet
    {
        public readonly IReadOnlyList<char> Terminals;
        public readonly IReadOnlyList<char> NotTerminals;
        public readonly char InitState;

        public Alphabet(char init, char[] term, char[] notTerm)
        {
            var _terminals = new char[term.Length];
            var _notTerminals = new char[notTerm.Length];

            term.CopyTo(_terminals, 0);
            notTerm.CopyTo(_notTerminals, 0);


            Terminals = new List<char>(_terminals);
            NotTerminals = new List<char>(_notTerminals);
            InitState = init;
            if (!_notTerminals.Contains(init))
                throw new Exception($"Начальный символ {init} должен содержаться в множестве нетерминалов!");
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
                        Rnt: alph.NotTerminals[_rand.Next(alph.NotTerminals.Count)]
                    ));
            for (int i = 1; i < ntRuleCount; ++i)
            {
                var curr = new Rule(
                        Lnt: alph.NotTerminals[_rand.Next(alph.NotTerminals.Count)],
                        Rt: alph.Terminals[_rand.Next(alph.Terminals.Count)],
                        Rnt: alph.NotTerminals[_rand.Next(alph.NotTerminals.Count)]
                    );
                if(!result.Contains(curr))
                    result.Add(curr);
            }
            for (int i = 0; i < tRuleCount; ++i)
            {
                var curr = new Rule(
                          Lnt: alph.NotTerminals[_rand.Next(alph.NotTerminals.Count)],
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