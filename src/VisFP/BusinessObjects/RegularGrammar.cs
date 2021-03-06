﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisFP.Utils;

namespace VisFP.BusinessObjects
{
    public class CantGenerateChainException : Exception
    {
        public CantGenerateChainException(string message) : base(message) { }
    }
    public class Rule : IEquatable<Rule> //TODO: удалить возможность ставить null в Rnt, всегда финальная вершина с меткой (исправить в алгоритмах!!!)
    {
        public readonly char Lnt; //левая часть правила - нетреминал
        public readonly char Rt; //правая часть правила - терминал
        public readonly char Rnt; //правая часть правила - нетерминал
        public readonly bool IsFinite;

        public Rule(char Lnt, char Rt, char Rnt, bool isFinite = false)
        {
            this.Lnt = Lnt;
            this.Rnt = Rnt;
            this.Rt = Rt;
            IsFinite = isFinite;
        }

        public override string ToString()
        {
            if (!IsFinite)
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
        public int MaxChainTry { get; set; } = 100;
        [JsonIgnore]
        public Lazy<char[]> CyclicNonterminals;
        [JsonIgnore]
        public Lazy<char[]> ReachableNonterminals;
        [JsonIgnore]
        public Lazy<char[]> GeneratingNonterminals;
        [JsonIgnore]
        public Lazy<RgNode> GrammarGraph;
        [JsonIgnore]
        private Lazy<bool> _isCyclic;
        [JsonIgnore]
        private Lazy<bool> _isDeterministic;

        public RegularGrammar(Alphabet alph, IReadOnlyList<Rule> rules)
        {
            Rules = rules;
            Alph = alph;
            CyclicNonterminals = new Lazy<char[]>(() => FindCyclicNonTerminals(), true);
            ReachableNonterminals = new Lazy<char[]>(() => FindReachableNonTerminals(), true);
            GeneratingNonterminals = new Lazy<char[]>(() => FindGeneratingNonTerminals(), true);
            GrammarGraph = new Lazy<RgNode>(() => GenerateGrammarGraph(), true);
            _isCyclic = new Lazy<bool>(() => CheckIsCyclic(), true);
            _isDeterministic = new Lazy<bool>(() => CheckIsDeterministic(), true);
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

        /// <summary>
        /// порождает ли пустой язык?
        /// </summary>
        [JsonIgnore]
        public bool IsEmptyLanguage 
        {
            get
            {
                return !GeneratingNonterminals.Value.Contains(Alph.InitState);
            }
        }

        /// <summary>
        /// Детерминированный?
        /// </summary>
        [JsonIgnore]
        public bool IsDeterministic
        {
            get
            {
                return _isDeterministic.Value;
            }
        }

        /// <summary>
        /// порождает ли бесконечный язык?
        /// </summary>
        [JsonIgnore]
        public bool IsInfinityLanguage 
        {
            get
            {
                return _isCyclic.Value;
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
                    .Where(x => !emptySymbols.Contains(x.Lnt) || (!x.IsFinite && !emptySymbols.Contains(x.Rnt))).ToList();
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
                    .Where(x => !unreachableSymbols.Contains(x.Lnt) || (!x.IsFinite && !unreachableSymbols.Contains(x.Rnt))).ToList();
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

        [Obsolete]
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
            for (int i = 0; i < MaxChainTry && !isFit; ++i) //несколько раз пытаемся сгенерить цепочку
            {
                int neededTerminalsCount = minLength - 1;
                rulesToProduce = new List<int>();
                output = new StringBuilder();
                RgNode currentNode = currentGrammar.GrammarGraph.Value;
                while (true)
                {
                    if (neededTerminalsCount > 0)
                    {
                        var nTStates = currentNode.Edges.Where(x => x.Value.NewState.NonTerminal != Alph.FiniteState).ToList();
                        if (nTStates.Count == 0)
                            break;
                        var selectedRule = nTStates[r.Next(0, nTStates.Count)];
                        output.Append(selectedRule.Value.Terminal); //Добавляем в выходную цепочку терминал
                        rulesToProduce.Add(selectedRule.Key); //Добавляем правило, которое использовали
                        currentNode = selectedRule.Value.NewState; //Меняем состояние на выбранное
                    }
                    else
                    {
                        var endState = currentNode.Edges.FirstOrDefault(x => x.Value.NewState.NonTerminal == Alph.FiniteState);
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
                ChainRules = string.Join(" ", rulesToProduce)
            };

        }

        /// <summary>
        /// Получение всех выводимых цепочек длины minLength, и если таковых нет, пытаемся получить все длины minLength+1.
        /// </summary>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public List<ChainResult> GetAllChains(int minLength)
        {
            if (!IsProper)
                throw new InvalidOperationException("Цепочки генерируются только по приведенной грамматике");
            List<Tuple<ChainResult, RgNode>> currentPathes = //сначала заливаем туда инициализирующее состояние
                new List<Tuple<ChainResult, RgNode>>(
                    new[] {
                        new Tuple<ChainResult,RgNode>(
                            new ChainResult
                            {
                                Chain = "",
                                ChainRules = ""
                            },
                            GrammarGraph.Value)
                    }
               );

            List<ChainResult> result = new List<ChainResult>();
            for (int i = 0; i < minLength || (currentPathes.Count == 0 && i < minLength*2); ++i)
            {
                List<Tuple<ChainResult, RgNode>> nextPathes = new List<Tuple<ChainResult, RgNode>>();
                foreach (var path in currentPathes)
                {
                    foreach(var edge in path.Item2.Edges)
                    {
                        if (edge.Value.NewState.NonTerminal == Alph.FiniteState)
                        {
                            if (i >= (minLength - 1))
                                result.Add(new ChainResult
                                {
                                    Chain = path.Item1.Chain + edge.Value.Terminal,
                                    ChainRules = $"{path.Item1.ChainRules} {edge.Key}"
                                });
                            if (Alph.NonTerminals.Contains(Alph.FiniteState)) //из финальной вершины тоже могут быть дуги (если вершина в нетерминалах)
                            {
                                nextPathes.Add(
                                new Tuple<ChainResult, RgNode>(new ChainResult
                                {
                                    Chain = path.Item1.Chain + edge.Value.Terminal,
                                    ChainRules = $"{path.Item1.ChainRules} {edge.Key}"
                                },
                                edge.Value.NewState));
                            }
                        }
                        else
                        {
                            nextPathes.Add(
                                new Tuple<ChainResult, RgNode>(new ChainResult
                                {
                                    Chain = path.Item1.Chain + edge.Value.Terminal,
                                    ChainRules = $"{path.Item1.ChainRules} {edge.Key}"
                                },
                                edge.Value.NewState));
                        }
                    }
                }
                currentPathes = nextPathes;
            }
            if (result.Count == 0)
                throw new CantGenerateChainException($"Нет завершенных цепочек длиной от {minLength} до {2*minLength}");

            return result;
        }

        public string GetStateSequenceForResult(string chainRules)
        {
            var rules = chainRules.Split(' ');
            List<char> result = new List<char>();
            result.Add(Alph.InitState);
            foreach(var rule in rules)
            {
                result.Add(Rules[int.Parse(rule) - 1].Rnt);
            }
            return string.Join(" ", result);
        }
        
        public List<string> RulesForChainRepresentable(string chain)
        {
            var node = GrammarGraph.Value;
            List<string> res = new List<string>();
            FindWay(res, node, "", chain);
            return res;
        }

        public List<string> GetTransitionFunc()
        {
            List<string> table = new List<string>();
            foreach (var rule in Rules.GroupBy(x => x.Lnt))
            {
                foreach (var row in rule.GroupBy(x => x.Rt))
                {
                    table.Add($"δ({rule.Key},{row.Key}) = {{{string.Join(",", row.Select(x => x.Rnt))}}}");
                }
            }
            return table;
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
            Alphabet alphabet;
            if (parsed[nameof(Alph)]["FiniteState"].Value<char>() == '$')
                alphabet = new Alphabet(
                    init: parsed[nameof(Alph)]["InitState"].Value<char>(),
                    term: parsed[nameof(Alph)]["Terminals"].ToObject<char[]>(),
                    notTerm: parsed[nameof(Alph)]["NonTerminals"].ToObject<char[]>());
            else
                alphabet = new Alphabet(
                    init: parsed[nameof(Alph)]["InitState"].Value<char>(),
                    term: parsed[nameof(Alph)]["Terminals"].ToObject<char[]>(),
                    notTerm: parsed[nameof(Alph)]["NonTerminals"].ToObject<char[]>(),
                    finite: parsed[nameof(Alph)]["FiniteState"].Value<char>());
            var rules = parsed[nameof(Rules)].ToObject<Rule[]>();
            return new RegularGrammar(alphabet, rules);
        }
        #endregion

        #region helpers
        private void FindWay(List<string> storage, RgNode currentNode, string rulesBefore, string chainTail)
        {
            if (chainTail.Length > 1)
            {
                foreach (var t in currentNode.Edges.Where(x => x.Value.Terminal == chainTail[0]))
                {
                    var rulesCurrent = $"{rulesBefore} {(t.Key + 1).ToString()}"; //начинаем с единицы
                    FindWay(storage, t.Value.NewState, rulesCurrent, chainTail.Substring(1));
                }
            }
            else //ищем терминальное правило с нужным терминалом
            {
                var finalRule = currentNode
                    .Edges
                    .FirstOrDefault(
                        x => x.Value.Terminal == chainTail[0]
                        && x.Value.NewState.NonTerminal == Alph.FiniteState);
                if (!finalRule.Equals(default(KeyValuePair<int, RgEdge>)))
                    storage.Add($"{rulesBefore} {(finalRule.Key + 1).ToString()}".Substring(1));//удаляем первый пробел
            }
        }
        private char[] FindCyclicNonTerminals()
        {
            List<char> cyclic = new List<char>();
            foreach (var r in Rules)
                if (r.Rnt == r.Lnt)
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
                    if (!reachable.Contains(nt.Rnt))
                    {
                        if (nt.IsFinite && nt.Rnt == '$')
                            continue;
                        reachable.Add(nt.Rnt);
                        q.Enqueue(nt.Rnt);
                    }
                }
            }
            return reachable.ToArray();
        }
        private char[] FindGeneratingNonTerminals()
        {
            HashSet<char> generating =
                new HashSet<char>(Rules
                    .Where(x => x.IsFinite)
                    .Select(x => x.Lnt)
                    .Distinct());
            var prevSetLength = 0;
            while (prevSetLength != generating.Count)
            {
                prevSetLength = generating.Count;
                foreach (var r in Rules.Where(x => !x.IsFinite && generating.Contains(x.Rnt)))
                    generating.Add(r.Lnt);
            }
            return generating.ToArray();
        }
        private Dictionary<char, RgNode> _allNodes;
        private RgNode GenerateGrammarGraph()
        {
            _allNodes = new Dictionary<char, RgNode>();
            _allNodes.Add(Alph.InitState, new RgNode(Alph.InitState)); //начальное состояние
            _allNodes.Add(Alph.FiniteState, new RgNode(Alph.FiniteState)); //конечное состояние
            foreach (var rule in Rules.Select((r, i) => new { rule = r, num = i }))
            {
                RgNode fromNode = _allNodes.AddOrGetRgNode(rule.rule.Lnt);
                RgNode toNode;
                toNode = _allNodes.AddOrGetRgNode(rule.rule.Rnt);
                fromNode.Edges.Add(
                    rule.num,
                    new RgEdge { NewState = toNode, Terminal = rule.rule.Rt });
            }
            return _allNodes[Alph.InitState];
        }

        //проверка на наличие циклов в графе
        private bool CheckIsCyclic()
        {
            if (CyclicNonterminals.Value.Length > 0)
            {
                if (CyclicNonterminals //если наш циклический символ достижим из начала и может дойти до конца
                    .Value
                    .Any(x => ReachableNonterminals.Value.Contains(x)
                        && GeneratingNonterminals.Value.Contains(x)))
                    return true;
                
            }
            var t = GrammarGraph.Value;
            Dictionary<char, byte> visitedNodes = new Dictionary<char, byte>();
            foreach (var S in Alph.NonTerminals)
                visitedNodes.Add(S, 0); //все белые
            bool cycleExists = false;
            foreach (var node in _allNodes
                .Select(x => x.Value)
                .Where(x => ReachableNonterminals.Value.Contains(x.NonTerminal)
                    && GeneratingNonterminals.Value.Contains(x.NonTerminal)))
                if (visitedNodes[node.NonTerminal] == 0)
                    ColorDfs(node, visitedNodes, ref cycleExists);
            return cycleExists;
        }

        private void ColorDfs(RgNode node, Dictionary<char, byte> states, ref bool cycleExist)
        {
            states[node.NonTerminal] = 1; //красим в серую
            foreach (var e in node.Edges)
            {
                var nt = e.Value.NewState.NonTerminal;
                if (states[nt] == 0)
                    ColorDfs(e.Value.NewState, states, ref cycleExist);
                if (states[nt] == 1)
                    cycleExist = true;
            }
            states[node.NonTerminal] = 2; //красим в черную
        }

        private bool CheckIsDeterministic()
        {
            foreach (var rule in Rules.GroupBy(x => x.Lnt))
            {
                foreach (var row in rule.GroupBy(x => x.Rt))
                {
                    if (row.Select(x => x.Rnt).Count() > 1)
                        return false;
                }
            }
            return true;
        }
        #endregion
    }
}
