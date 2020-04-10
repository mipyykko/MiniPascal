using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Common
{
    public class Rule
    {
        public StatementType Name { get; private set; }
        public Production Production { get; private set; }
        public Parameters Parameters { get; private set; }
        public Func<dynamic[], dynamic> GathererFn { get; private set; }
        public Gatherer Gatherer => Gatherer.Of(this);

        public Rule()
        {
        }

        private Rule(StatementType name, Production production, Parameters parameters, Func<dynamic[], dynamic> gathererFn)
        {
            Name = name;
            Production = production;
            Parameters = parameters;
            GathererFn = gathererFn == null ? (dynamic[] p) => null : gathererFn;
        }


        public static Rule Of(StatementType name, Production production, Parameters parameters, Func<dynamic[], dynamic> gathererFn) =>
            new Rule(name, production, parameters, gathererFn);

        public override string ToString()
        {
            return $"Rule {Name}: {string.Join(", ", Production.Items)}";
        }
    }
    
    public class Gatherer
    {
        public Rule Rule;
        private Func<dynamic[], dynamic> Fn;

        private readonly List<dynamic> Collected = new List<dynamic>();
        public bool AllCollected => paramIdx == Rule.Parameters.Items.Length;

        private int paramIdx = 0;
        
        public void Add(dynamic item)
        {
            if (Rule.Parameters.Items.Length != Rule.Production.Items.Length)
            {
                throw new Exception($"illegal rule {Rule}");
            }
            if (Rule.Parameters.Items.Length > 0 && paramIdx >= Rule.Parameters.Items.Length)
            {
                throw new Exception($"too many params {Rule} - collected {string.Join(", ", Collected.ToArray())}, got {item}");
            }

            var currRule = Rule.Parameters[paramIdx];
            
            Console.WriteLine($"Waiting for {Rule.Production[paramIdx]}, got {item}");
            if (currRule)
            {
                Console.WriteLine($"Matched {item}");
                Collected.Add(item);
            }
            paramIdx++;
        }

        public dynamic Result
        {
            get
            {
                Console.WriteLine($"Result of {Rule}: {string.Join(", ", Collected.ToArray())}");

                return Rule.GathererFn(Collected.Flatten().ToArray());;
            }
        }

        private Gatherer(Rule rule)
        {
            Rule = rule;
        }

        public static Gatherer Of(Rule rule) => new Gatherer(rule);

        public override string ToString()
        {
            return $"Gatherer {Rule}, collected {string.Join(",", Collected.ToArray())}\n";
        }
    }

}
