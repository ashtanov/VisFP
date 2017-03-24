﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisFP.Utils;

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
    public class ChainResult
    {
        public string Chain { get; set; }
        public string ChainRules { get; set; }
    }

    [JsonObject]
    public class RegularGrammar
    {
        public class RgEdge
        {
            public char Terminal { get; set; }
            public RgNode NewState { get; set; }
        }
        public class RgNode
        {
            public readonly char NonTerminal;
            public readonly Dictionary<int, RgEdge> Edges; // ключ - номер правила
            public RgNode(char nonTerminal)
            {
                NonTerminal = nonTerminal;
                Edges = new Dictionary<int, RgEdge>();
            }

        }
        public IReadOnlyList<Rule> Rules { get; private set; }
        public Alphabet Alph { get; private set; }
        public readonly char EndState = '$';
        public int MaxChainTry { get; set; } = 10;
        [JsonIgnore]
        public Lazy<char[]> CyclicNonterminals;
        [JsonIgnore]
        public Lazy<char[]> ReachableNonterminals;
        [JsonIgnore]
        public Lazy<char[]> GeneratingNonterminals;
        [JsonIgnore]
        public Lazy<RgNode> GrammarGraph;

        public RegularGrammar(Alphabet alph, IReadOnlyList<Rule> rules)
        {
            Rules = rules;
            Alph = alph;
            CyclicNonterminals = new Lazy<char[]>(() => FindCyclicNonTerminals(), true);
            ReachableNonterminals = new Lazy<char[]>(() => FindReachableNonTerminals(), true);
            GeneratingNonterminals = new Lazy<char[]>(() => FindGeneratingNonTerminals(), true);
            GrammarGraph = new Lazy<RgNode>(() => GenerateGrammarGraph(), true);
        }
        [JsonIgnore]
        public bool IsProper //приведенная ли?
        {
            get
            {
                return
                    ReachableNonterminals.Value.Length == Alph.NonTerminals.Count
                    && GeneratingNonterminals.Value.Length == Alph.NonTerminals.Count;
            }
        }

        [JsonIgnore]
        public bool IsEmptyLanguage //порождает ли пустой язык?
        {
            get
            {
                return !GeneratingNonterminals.Value.Contains(Alph.InitState);
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
                var emptySymbols = new HashSet<char>(Alph.NonTerminals.Except(GeneratingNonterminals.Value));
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
                var unreachableSymbols = new HashSet<char>(tmp.Alph.NonTerminals.Except(tmp.ReachableNonterminals.Value));
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
            catch (ArgumentException ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Генерирует цепочку нетерминалов, допустимую грамматикой.
        /// </summary>
        /// <param name="minLength">Минимальная длина цепочки</param>
        /// <returns></returns>
        public ChainResult GenerateRandomChain(int minLength)
        {
            Random r = new Random();
            RegularGrammar currentGrammar;
            if (!IsProper)
            {
                throw new InvalidOperationException("Цепочки генерируются только по приведенной грамматике");
                //currentGrammar = GetProperVersion();
                //if(currentGrammar != null)
                //    throw new InvalidOperationException("Грамматика не представима в приведенной форме");
            }
            else
                currentGrammar = this;
            bool isFit = false;
            StringBuilder output = new StringBuilder();
            List<int> rulesToProduce = new List<int>();
            for (int i = 0; i < MaxChainTry && !isFit; ++i) //10 раз пытаемся сгенерить цепочку
            {
                int neededTerminalsCount = minLength;
                rulesToProduce = new List<int>();
                output = new StringBuilder();
                RgNode currentNode = currentGrammar.GrammarGraph.Value;
                while (true)
                {
                    if (neededTerminalsCount > 0)
                    {
                        var nTStates = currentNode.Edges.Where(x => x.Value.NewState.NonTerminal != EndState).ToList();
                        if (nTStates.Count == 0)
                            break;
                        var selectedRule = nTStates[r.Next(0, nTStates.Count)];
                        output.Append(selectedRule.Value.Terminal); //Добавляем в выходную цепочку терминал
                        rulesToProduce.Add(selectedRule.Key); //Добавляем правило, которое использовали
                        currentNode = selectedRule.Value.NewState; //Меняем состояние на выбранное
                    }
                    else
                    {
                        var endState = currentNode.Edges.FirstOrDefault(x => x.Value.NewState.NonTerminal == EndState);
                        if (endState.Equals(default(KeyValuePair<int, RgEdge>))) //если из текущего состояния нет терминального правила
                        {
                            if (output.Length == 2 * minLength) //если наша цепочка в 2 раза больше минимальной - пробуем сначала
                                break;
                            var nTStates = currentNode.Edges.ToList();
                            var selectedRule = nTStates[r.Next(0, nTStates.Count)];
                            output.Append(selectedRule.Value.Terminal); //Добавляем в выходную цепочку терминал
                            rulesToProduce.Add(selectedRule.Key); //Добавляем правило, которое использовали
                            currentNode = selectedRule.Value.NewState; //Меняем состояние на выбранное
                        }
                        else
                        {
                            output.Append(endState.Value.Terminal); //Добавляем в выходную цепочку терминал
                            rulesToProduce.Add(endState.Key); //Добавляем правило, которое использовали
                            isFit = true; //получили нужную цепочку
                            break;
                        }
                    }
                    neededTerminalsCount--;
                }
            }
            if (!isFit)
                throw new Exception($"Не удалось сгенерить цепочку за {MaxChainTry} раз");
            return new ChainResult
            {
                Chain = output.ToString(),
                ChainRules = string.Join(",", rulesToProduce)
            };

        }

        private char[] FindCyclicNonTerminals()
        {
            List<char> cyclic = new List<char>();
            foreach (var r in Rules)
                if (r.Rnt.HasValue && r.Rnt.Value == r.Lnt)
                    cyclic.Add(r.Lnt);
            return cyclic.Distinct().ToArray();
        }
        private char[] FindReachableNonTerminals()
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
            return reachable.ToArray();
        }
        private char[] FindGeneratingNonTerminals()
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
            return generating.ToArray();
        }
        private RgNode GenerateGrammarGraph()
        {
            Dictionary<char, RgNode> suppDict = new Dictionary<char, RgNode>();
            suppDict.Add(Alph.InitState, new RgNode(Alph.InitState)); //начальное состояние
            suppDict.Add(EndState, new RgNode(EndState)); //конечное состояние
            foreach (var rule in Rules.Select((r, i) => new { rule = r, num = i }))
            {
                RgNode fromNode = suppDict.AddOrGetRgNode(rule.rule.Lnt);
                RgNode toNode;
                if (rule.rule.Rnt.HasValue)
                    toNode = suppDict.AddOrGetRgNode(rule.rule.Rnt.Value);
                else
                    toNode = suppDict[EndState];
                fromNode.Edges.Add(
                    rule.num,
                    new RgEdge { NewState = toNode, Terminal = rule.rule.Rt });
            }
            return suppDict[Alph.InitState];
        }

        #region Serialize|Deserialize
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
        /// <summary>
        /// Парсим граматику из жсона. МОжет кинуть исключение, если в алфавите нетерминалов нет инициализирующего символа
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
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
        #endregion
    }
}
