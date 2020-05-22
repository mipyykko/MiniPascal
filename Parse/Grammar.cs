using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Scan;
using StatementType = Common.StatementType;
using static Parse.ParseTree;

namespace Parse
{
    public static class Grammar
    {
        private static dynamic Pass(dynamic[] p)
        {
            return p.GetType().IsArray ? p.Length == 1 ? p[0] : p : new[] {p};
        }

        private static dynamic[] Flatten(dynamic[] p)
        {
            return p.Flatten().ToArray();
        }

        private static dynamic[] PassT(dynamic[] p)
        {
            return p;
        }

        public static Rule ErrorRule =
            Rule.Of(StatementType.Error, Production.Of(TokenType.Error), Collect.Of(true), Error);
        
        public static readonly Rule[] Rules =
        {
            Rule.Of(StatementType.ProgramStatement,
                Production.Of(KeywordType.Program, StatementType.Identifier, TokenType.Separator,
                    StatementType.ProgramCont), // StatementType.DeclarationBlock, StatementType.Block, TokenType.Dot), 
                Collect.Of(false, true, false, true),
                Program),
            Rule.Of(StatementType.ProgramCont,
                Production.Of(StatementType.Block, TokenType.Dot),
                Collect.Of(true, false),
                Pass),
            Rule.Of(StatementType.ProgramCont,
                Production.Of(StatementType.DeclarationBlock, StatementType.Block, TokenType.Dot),
                Collect.Of(true, true, false),
                Pass),
            Rule.Of(StatementType.DeclarationBlock,
                Production.Of(StatementType.ProcedureDeclaration, StatementType.DeclarationBlockCont),
                Collect.Of(true, true),
                DeclarationBlockStatement),
            Rule.Of(StatementType.DeclarationBlock,
                Production.Of(StatementType.FunctionDeclaration, StatementType.DeclarationBlockCont),
                Collect.Of(true, true),
                DeclarationBlockStatement),
            Rule.Of(StatementType.DeclarationBlockCont,
                Production.EpsilonProduction,
                Collect.None,
                Pass),
            Rule.Of(StatementType.DeclarationBlockCont,
                Production.Of(StatementType.DeclarationBlock, StatementType.DeclarationBlockCont),
                Collect.Of(true, true),
                Flatten),
            Rule.Of(StatementType.Declaration,
                Production.Of(StatementType.ProcedureDeclaration),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Declaration,
                Production.Of(StatementType.FunctionDeclaration),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Declaration,
                Production.Of(KeywordType.Var, StatementType.VariableIds, TokenType.Colon, StatementType.Type),
                Collect.Of(false, true, false, true), VarDeclaration),
            Rule.Of(StatementType.ProcedureDeclaration,
                Production.Of(KeywordType.Procedure, StatementType.Identifier, TokenType.OpenParen,
                    StatementType.Parameters, TokenType.CloseParen, TokenType.Separator, StatementType.Block,
                    TokenType.Separator),
                Collect.Of(false, true, false, true, false, false, true, false),
                ProcedureDeclaration),
            Rule.Of(StatementType.FunctionDeclaration,
                Production.Of(KeywordType.Function, StatementType.Identifier, TokenType.OpenParen,
                    StatementType.Parameters, TokenType.CloseParen, TokenType.Colon, StatementType.Type,
                    TokenType.Separator, StatementType.Block, TokenType.Separator),
                Collect.Of(false, true, false, true, false, false, true, false, true, false),
                FunctionDeclaration),
            Rule.Of(StatementType.Identifier,
                Production.Of(TokenType.Identifier),
                Collect.Of(true),
                Identifier),
            Rule.Of(StatementType.VariableIds,
                Production.Of(StatementType.Identifier, StatementType.Ids),
                Collect.Of(true, true),
                ListBuilder),
            Rule.Of(StatementType.Ids, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.Ids,
                Production.Of(TokenType.Comma, StatementType.Identifier, StatementType.Ids),
                Collect.Of(false, true, true),
                ListBuilder),
            Rule.Of(StatementType.Parameter,
                Production.Of(KeywordType.Var, StatementType.Identifier, TokenType.Colon, StatementType.Type),
                Collect.Of(true, true, false, true),
                Parameter),
            Rule.Of(StatementType.Parameter,
                Production.Of(StatementType.Identifier, TokenType.Colon, StatementType.Type),
                Collect.Of(true, false, true),
                Parameter),
            Rule.Of(StatementType.Parameters, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.Parameters,
                Production.Of(StatementType.Parameter, StatementType.OptParameters),
                Collect.Of(true, true),
                ListBuilder),
            Rule.Of(StatementType.OptParameters, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.OptParameters,
                Production.Of(TokenType.Comma, StatementType.Parameters),
                Collect.Of(false, true),
                Pass),
            Rule.Of(StatementType.Type,
                Production.Of(StatementType.SimpleType),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Type,
                Production.Of(StatementType.ArrayType),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleType,
                Production.Of(StatementType.TypeId),
                Collect.Of(true),
                SimpleType),
            Rule.Of(StatementType.ArrayType,
                Production.Of(KeywordType.Array, TokenType.OpenBlock, StatementType.ArrayTypeCont),
                Collect.Of(false, false, true),
                ArrayType),
            Rule.Of(StatementType.ArrayTypeCont,
                Production.Of(StatementType.IntegerExpr, TokenType.CloseBlock, KeywordType.Of,
                    StatementType.SimpleType),
                Collect.Of(true, false, false, true),
                Pass),
            Rule.Of(StatementType.ArrayTypeCont,
                Production.Of(TokenType.CloseBlock, KeywordType.Of, StatementType.SimpleType),
                Collect.Of(false, false, true),
                Pass),
            Rule.Of(StatementType.Block,
                Production.Of(KeywordType.Begin, StatementType.Statement, StatementType.StatementListBlockEnd),
                Collect.Of(false, true, true),
                BlockStatement),
            Rule.Of(StatementType.StatementListBlockEnd,
                Production.Of(TokenType.Separator, StatementType.StatementOrBlockEnd),
                Collect.Of(false, true),
                Pass),
            Rule.Of(StatementType.StatementListBlockEnd,
                Production.Of(KeywordType.End),
                Collect.Of(false),
                Pass),
            Rule.Of(StatementType.StatementOrBlockEnd,
                Production.Of(StatementType.Statement, StatementType.StatementListBlockEnd),
                Collect.Of(true, true),
                BlockStatement),
            Rule.Of(StatementType.StatementOrBlockEnd,
                Production.Of(KeywordType.End),
                Collect.Of(false),
                Pass),
            Rule.Of(StatementType.Statement,
                Production.Of(StatementType.SimpleStatement),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Statement,
                Production.Of(StatementType.StructuredStatement),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Statement,
                Production.Of(StatementType.Declaration),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleStatement,
                Production.Of(StatementType.AssignOrCallStatement),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleStatement,
                Production.Of(StatementType.ReturnStatement),
                Collect.Of(true),
                ReturnStatement),
            Rule.Of(StatementType.SimpleStatement,
                Production.Of(StatementType.AssertStatement),
                Collect.Of(true),
                AssertStatement),
            Rule.Of(StatementType.SimpleStatement,
                Production.Of(StatementType.ReadStatement),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleStatement,
                Production.Of(StatementType.WriteStatement),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.AssignOrCallStatement,
                Production.Of(StatementType.Identifier, StatementType.AssignmentStatementOrCall),
                Collect.Of(true, true),
                AssignOrCallStatement),
            Rule.Of(StatementType.AssignmentStatementOrCall,
                Production.Of(TokenType.OpenBlock, StatementType.IntegerExpr, TokenType.CloseBlock,
                    TokenType.Assignment, StatementType.Expr),
                Collect.Of(false, true, false, false, true),
                AssignmentToArrayStatement),
            Rule.Of(StatementType.AssignmentStatementOrCall,
                Production.Of(TokenType.Assignment, StatementType.Expr),
                Collect.Of(false, true),
                AssignmentStatement),
            Rule.Of(StatementType.AssignmentStatementOrCall,
                Production.Of(TokenType.OpenParen, StatementType.Arguments, TokenType.CloseParen),
                Collect.Of(false, true, false),
                CallStatement),
            Rule.Of(StatementType.Arguments, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.Arguments,
                Production.Of(StatementType.Expr, StatementType.Exprs),
                Collect.Of(true, true),
                ListBuilder),
            Rule.Of(StatementType.Exprs, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.Exprs,
                Production.Of(TokenType.Comma, StatementType.Expr, StatementType.Exprs),
                Collect.Of(false, true, true),
                ListBuilder),
            Rule.Of(StatementType.ReturnStatement,
                Production.Of(KeywordType.Return, StatementType.OptReturnExpr),
                Collect.Of(false, true),
                Pass),
            Rule.Of(StatementType.OptReturnExpr, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.OptReturnExpr,
                Production.Of(StatementType.Expr),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.ReadStatement,
                Production.Of(KeywordType.Read, TokenType.OpenParen, StatementType.Variable, StatementType.Variables,
                    TokenType.CloseParen),
                Collect.Of(false, false, true, true, false),
                Pass), // TODO
            Rule.Of(StatementType.Variables, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.Variables,
                Production.Of(TokenType.Comma, StatementType.Variable, StatementType.Variables),
                Collect.Of(false, true, true),
                Flatten),
            Rule.Of(StatementType.WriteStatement,
                Production.Of(KeywordType.WriteLn, TokenType.OpenParen, StatementType.Arguments, TokenType.CloseParen),
                Collect.Of(false, false, true, false),
                Pass), // TODO
            Rule.Of(StatementType.AssertStatement,
                Production.Of(KeywordType.Assert, TokenType.OpenParen, StatementType.BooleanExpr, TokenType.CloseParen),
                Collect.Of(false, false, true, false),
                Pass), // TODO
            Rule.Of(StatementType.StructuredStatement,
                Production.Of(StatementType.Block),
                Collect.Of(true), Pass),
            Rule.Of(StatementType.StructuredStatement,
                Production.Of(StatementType.IfStatement),
                Collect.Of(true), Pass),
            Rule.Of(StatementType.StructuredStatement,
                Production.Of(StatementType.WhileStatement),
                Collect.Of(true), Pass),
            Rule.Of(StatementType.IfStatement,
                Production.Of(KeywordType.If, StatementType.BooleanExpr, KeywordType.Then, StatementType.Statement,
                    StatementType.ElseBranch),
                Collect.Of(false, true, false, true, true),
                IfStatement),
            Rule.Of(StatementType.ElseBranch, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.ElseBranch,
                Production.Of(KeywordType.Else, StatementType.Statement),
                Collect.Of(false, true),
                Pass),
            Rule.Of(StatementType.WhileStatement,
                Production.Of(KeywordType.While, StatementType.BooleanExpr, KeywordType.Do, StatementType.Statement),
                Collect.Of(false, true, false, true),
                WhileStatement),
            Rule.Of(StatementType.Expr,
                Production.Of(StatementType.SimpleExpr, StatementType.ExprCont),
                Collect.Of(true, true),
                Expr),
            Rule.Of(StatementType.ExprCont, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.ExprCont,
                Production.Of(StatementType.RelationalOperator, StatementType.SimpleExpr),
                Collect.Of(true, true),
                Pass),
            Rule.Of(StatementType.SignTerm,
                Production.Of(StatementType.Sign, StatementType.Term),
                Collect.Of(true, true),
                SignTerm),
            Rule.Of(StatementType.SignTerm,
                Production.Of(StatementType.Term),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleExpr,
                Production.Of(StatementType.SignTerm, StatementType.SimpleExprCont),
                Collect.Of(true, true),
                SimpleExprOrTerm),
            Rule.Of(StatementType.SimpleExprCont, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.SimpleExprCont,
                Production.Of(StatementType.AddingOperator, StatementType.Term, StatementType.SimpleExprCont),
                Collect.Of(true, true, true),
                Flatten),
            Rule.Of(StatementType.Term,
                Production.Of(StatementType.Factor, StatementType.TermCont),
                Collect.Of(true, true),
                SimpleExprOrTerm),
            Rule.Of(StatementType.TermCont, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.TermCont,
                Production.Of(StatementType.MultiplyingOperator, StatementType.Factor, StatementType.TermCont),
                Collect.Of(true, true, true),
                Pass),
            Rule.Of(StatementType.IntegerExpr,
                Production.Of(StatementType.Expr),
                Collect.Of(true),
                Expr), // TODO?
            Rule.Of(StatementType.BooleanExpr,
                Production.Of(StatementType.Expr),
                Collect.Of(true),
                Expr), // TODO?
            Rule.Of(StatementType.Factor,
                Production.Of(StatementType.CallOrVariable, StatementType.FactorSize),
                Collect.Of(true, true),
                FactorOptSize),
            Rule.Of(StatementType.Factor,
                Production.Of(StatementType.Literal),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Factor,
                Production.Of(TokenType.OpenParen, StatementType.Expr, TokenType.CloseParen, StatementType.FactorSize),
                Collect.Of(false, true, false, true),
                FactorOptSize),
            Rule.Of(StatementType.Factor,
                Production.Of(KeywordType.Not, StatementType.Factor),
                Collect.Of(true, true),
                Unary),
            Rule.Of(StatementType.FactorSize, Production.EpsilonProduction, Collect.None, null),
            Rule.Of(StatementType.FactorSize,
                Production.Of(TokenType.Dot, "size"),
                Collect.Of(false, true),
                Pass), // TODO: hmm
            Rule.Of(StatementType.CallOrVariable,
                Production.Of(StatementType.Identifier, StatementType.CallOrVariableCont),
                Collect.Of(true, true),
                CallOrVariable),
            Rule.Of(StatementType.CallOrVariableCont,
                Production.Of(StatementType.VariableCont),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.CallOrVariableCont,
                Production.Of(TokenType.OpenParen, StatementType.Arguments, TokenType.CloseParen),
                Collect.Of(false, true, false),
                CallStatement),
            Rule.Of(StatementType.Variable,
                Production.Of(StatementType.Identifier, StatementType.VariableCont),
                Collect.Of(true, true),
                VariableOrArrayDeference),
            Rule.Of(StatementType.VariableCont,
                Production.EpsilonProduction,
                Collect.None,
                null),
            Rule.Of(StatementType.VariableCont,
                Production.Of(TokenType.OpenBlock, StatementType.IntegerExpr, TokenType.CloseBlock),
                Collect.Of(false, true, false),
                Pass),
            Rule.Of(StatementType.RelationalOperator,
                Production.Of("="),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.RelationalOperator,
                Production.Of("<>"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.RelationalOperator,
                Production.Of("<"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.RelationalOperator,
                Production.Of("<="),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.RelationalOperator,
                Production.Of(">="),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.RelationalOperator,
                Production.Of(">"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Sign,
                Production.Of("+"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Sign,
                Production.Of("-"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.AddingOperator,
                Production.Of("+"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.AddingOperator,
                Production.Of("-"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.AddingOperator,
                Production.Of("or"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.MultiplyingOperator,
                Production.Of("*"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.MultiplyingOperator,
                Production.Of("/"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.MultiplyingOperator,
                Production.Of("%"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.MultiplyingOperator,
                Production.Of("and"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.Literal,
                Production.Of(StatementType.NumberLiteral),
                Collect.Of(true),
                Literal),
            Rule.Of(StatementType.Literal,
                Production.Of(StatementType.StringLiteral),
                Collect.Of(true),
                Literal),
            Rule.Of(StatementType.Literal,
                Production.Of(StatementType.BooleanLiteral),
                Collect.Of(true),
                Literal),
            Rule.Of(StatementType.NumberLiteral,
                Production.Of(TokenType.IntegerValue),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.NumberLiteral,
                Production.Of(TokenType.RealValue),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.StringLiteral,
                Production.Of(TokenType.StringValue),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.BooleanLiteral,
                Production.Of(TokenType.BooleanValue),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.TypeId,
                Production.Of("integer"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.TypeId,
                Production.Of("real"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.TypeId,
                Production.Of("string"),
                Collect.Of(true),
                Pass),
            Rule.Of(StatementType.TypeId,
                Production.Of("boolean"),
                Collect.Of(true),
                Pass)
        };

        private static readonly HashSet<dynamic> Terminals = new HashSet<dynamic>();
        private static readonly HashSet<dynamic> NonTerminals = new HashSet<dynamic>();
        private static readonly DefaultDictionary<dynamic, bool> Epsilons = new DefaultDictionary<dynamic, bool>();

        private static readonly DefaultDictionary<dynamic, HashSet<dynamic>> First =
            new DefaultDictionary<dynamic, HashSet<dynamic>>();

        private static readonly DefaultDictionary<dynamic, HashSet<dynamic>> Follow =
            new DefaultDictionary<dynamic, HashSet<dynamic>>();

        public static readonly DefaultDictionary<dynamic, DefaultDictionary<dynamic, Rule>> Predictions =
            new DefaultDictionary<dynamic, DefaultDictionary<dynamic, Rule>>();

        private static bool IsTerminal(dynamic i)
        {
            return !(i is Production.Epsilon) && !IsNonTerminal(i);
        }

        private static bool IsNonTerminal(dynamic i)
        {
            return !(i is Production.Epsilon) && NonTerminals.Contains(i);
        }

        private static IEnumerable<dynamic> FilterEpsilon(IEnumerable<dynamic> s)
        {
            return new HashSet<dynamic>(s.Where(i => !(i is Production.Epsilon)));
        }

        public static void CreateGrammar()
        {
            var unused = Rules.Select(r => r.Name).ToHashSet();

            // grammophone output
            //foreach (var rule in Rules)
            //    Console.WriteLine(
            //        $"{rule.Name} -> {string.Join(" ", rule.Production.Items.Select(i => i is Production.Epsilon ? "" : i is StatementType ? i : i.ToString().ToLower()).ToArray())}.");

            var ruleDictionary = new Dictionary<string, List<string>>();

            foreach (var rule in Rules)
            {
                var ruleName = rule.Name.ToString();
                if (!ruleDictionary.ContainsKey(ruleName))
                {
                    ruleDictionary[ruleName] = new List<string>();
                }
                ruleDictionary[ruleName].Add(string.Join(
                    "\\ ", 
                    rule.Production.Items.Select(
                        i => i switch
                        {
                            Production.Epsilon => "\\varepsilon",
                            StatementType _ => $"<{i}>",
                            TokenType _ => $"<{i}>",
                            KeywordType _ => $"\\texttt{{{i}}}",
                            _ => i.ToString().ToLower()
                        }).ToArray())
                    );
            }

            foreach (var (name, productions) in ruleDictionary)
            {
                Console.WriteLine($"&<{name}>&\\quad &::= &\\quad &{string.Join("\\\\ \n&&\\quad &\\quad| &\\quad &", productions)} \\\\");
            }
            foreach (var rule in Rules)
            {
                NonTerminals.Add(rule.Name);
                foreach (var production in rule.Production.Items)
                {
                    if (production is StatementType) unused.Remove(production);
                    if (!(production is StatementType)) Terminals.Add(production); // not necessarily true?
                }
            }

            foreach (var (nonTerminal, productions) in Rules.Select(r => (r.Name, r.Production.Items)))
            {
                if (Epsilons[nonTerminal]) continue;
                if (productions.Any(p => p is Production.Epsilon)) Epsilons[nonTerminal] = true;
            }

            Epsilons[Production.Epsilon] = true;

            CalculateFirst();
            CalculateFollow();
            CalculatePredictions();

            foreach (var key in Predictions.Keys)
            foreach (var key2 in Predictions[key].Keys)
                Console.WriteLine($"{key}: {key2} -> {{{string.Join(", ", Predictions[key][key2].Production.Items)}}}");
        }

        private static void CalculateFirst()
        {
            foreach (var terminal in Terminals) First[terminal].Add(terminal);

            var changed = true;

            while (changed)
            {
                changed = false;

                foreach (var rule in Rules)
                {
                    var nonTerminal = rule.Name;
                    var set = First[nonTerminal];
                    var originalSize = set.Count;

                    rule.Production.Items.Select((i, idx) => (i, idx)).All(tuple =>
                    {
                        var (item, index) = tuple;
                        if (IsNonTerminal(item))
                        {
                            set.UnionWith(FilterEpsilon(First[item]));

                            if (First[item].Contains(Production.Epsilon))
                            {
                                if (index + 1 < rule.Production.Items.Length) return true;
                                set.Add(Production.Epsilon);
                            }
                        }
                        else if (IsTerminal(item))
                        {
                            set.Add(item);
                        }
                        else
                        {
                            set.Add(Production.Epsilon);
                        }

                        return false;
                    });

                    if (set.Count != originalSize)
                    {
                        First[nonTerminal] = set;
                        changed = true;
                    }
                }
            }
        }

        private static void CalculateFollow()
        {
            var changed = true;

            while (changed)
            {
                changed = false;

                foreach (var rule in Rules)
                {
                    var nonTerminal = rule.Name;

                    foreach (var (item, index) in rule.Production.Items.Select((i, idx) => (i, idx)))
                    {
                        if (!IsNonTerminal(item)) continue;

                        var rest = rule.Production.Items.Skip(index + 1).ToArray();
                        var set = Follow[item];
                        var originalSize = set.Count;

                        if (rest.Any())
                            rest.Select((i, idx) => (i, idx)).All(tuple =>
                            {
                                var (restItem, restIndex) = tuple;
                                if (IsNonTerminal(restItem))
                                {
                                    set.UnionWith(FilterEpsilon(First[restItem]));

                                    if (First[item].Contains(Production.Epsilon))
                                    {
                                        if (restIndex + 1 < rest.Length) return true;

                                        set.UnionWith(Follow[nonTerminal]);
                                    }
                                }
                                else
                                {
                                    set.Add(restItem);
                                }

                                return false;
                            });
                        else
                            set.UnionWith(Follow[nonTerminal]);

                        if (set.Count != originalSize)
                        {
                            Follow[item] = set;
                            changed = true;
                        }
                    }
                }
            }
        }

        private static void CalculatePredictions()
        {
            foreach (var rule in Rules)
            {
                var set = new HashSet<dynamic>();
                var nonTerminal = rule.Name;

                rule.Production.Items.Select((i, idx) => (i, idx)).All(tuple =>
                {
                    var (item, index) = tuple;

                    set.UnionWith(First[item]);
                    if (!Epsilons[item]) return false;

                    set.UnionWith(index + 1 < rule.Production.Items.Length
                        ? (IEnumerable<dynamic>) First[rule.Production[index + 1]]
                        : (IEnumerable<dynamic>) Follow[item]);
                    return true; //false;
                });

                foreach (var s in set)
                {
                    if (Predictions[nonTerminal].ContainsKey(s))
                        Console.WriteLine($"ambiguous rule {nonTerminal} -> {s}");
                    Predictions[nonTerminal][s] = rule;
                }
            }
        }
    }
}