using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.BusinessObjects
{
    public class FiniteStateMachine : RegularGrammar
    {
        //Пример. Автомат K1 = ({s, u, x, y, t} {0, 1}, δ, s, {t}), где функция перехода δ:
        //δ(s, 1) = {u, t}, δ(u, 0) = {x, y}, δ(x, 0) = {u}, δ(y, 0) = {t}.

        public FiniteStateMachine(Alphabet alph, IReadOnlyList<Rule> rules) : base(alph, rules)
        {
            if (Alph.FiniteState != default(char))
                EndState = Alph.FiniteState;
        }

        public List<string> GetTransitionTable()
        {
            List<string> table = new List<string>();
            foreach (var rule in Rules.GroupBy(x => x.Lnt))
            {
                foreach (var row in rule.GroupBy(x => x.Rt))
                {
                    table.Add($"δ({rule.Key},{row.Key}) = {{{string.Join(",", row.Select(x => x.Rnt.HasValue ? x.Rnt : EndState))}}}");
                }
            }
            return table;
        }

    }
}
