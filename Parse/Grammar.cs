using System;
using System.Collections.Generic;
using System.Linq;
using AST;
using Common;
using Scan;
using StatementType = Common.StatementType;
using static Parse.ParseTree;

namespace Parse
{
    public static class Grammar
    {
        private static dynamic Pass(dynamic[] p) => p.GetType().IsArray ? p.Length == 1 ? p[0] : p : p;
        private static dynamic[] Flatten(dynamic[] p) => p.Flatten().ToArray();
        
        public static Rule[] Rules = {
            Rule.Of(StatementType.ProgramStatement, 
                Production.Of(KeywordType.Program, StatementType.Identifier, TokenType.Separator, StatementType.Block, TokenType.Dot), 
                Parameters.Of(false, true, false, true, false), Program),
            Rule.Of(StatementType.Declaration, 
                Production.Of(KeywordType.Var, StatementType.VariableIds, TokenType.Colon, StatementType.Type), 
                Parameters.Of(false, true, false, true), VarDeclaration),
            Rule.Of(StatementType.Declaration, 
                Production.Of(KeywordType.Procedure, StatementType.Identifier, TokenType.OpenParen, StatementType.Parameters, TokenType.CloseParen, TokenType.Separator, StatementType.Block, TokenType.Separator), 
                Parameters.Of(false, true, false, true, false, false, true, false), null),
            Rule.Of(StatementType.Declaration, 
                Production.Of(KeywordType.Function, StatementType.Identifier, TokenType.OpenParen, StatementType.Parameters, TokenType.CloseParen, TokenType.Colon, StatementType.Type, TokenType.Separator, StatementType.Block, TokenType.Separator), 
                Parameters.Of(false, true, false, true, false, false, true, false, true, false), null),
            Rule.Of(StatementType.Identifier,
                Production.Of(TokenType.Identifier),
                Parameters.Of(true), 
                Identifier),
            Rule.Of(StatementType.VariableIds, 
                Production.Of(StatementType.Identifier, StatementType.Ids), Parameters.Of(true, true), 
                Flatten),
            Rule.Of(StatementType.Ids, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.Ids, 
                Production.Of(TokenType.Comma, StatementType.Identifier, StatementType.Ids), 
                Parameters.Of(false, true, true), 
                Flatten),
            Rule.Of(StatementType.Parameters, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.Parameters, 
                Production.Of(KeywordType.Var, StatementType.Identifier, TokenType.Colon, StatementType.Type, StatementType.OptParameters), 
                Parameters.Of(false, true, false, true, true), null),
            Rule.Of(StatementType.Parameters, 
                Production.Of(StatementType.Identifier, TokenType.Colon, StatementType.Type, StatementType.OptParameters), 
                Parameters.Of(true, false, true, true), null),
            Rule.Of(StatementType.OptParameters, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.OptParameters, 
                Production.Of(TokenType.Comma, StatementType.Parameters), 
                Parameters.Of(false, true), null),
            Rule.Of(StatementType.Type, 
                Production.Of(StatementType.SimpleType), 
                Parameters.Of(true), 
                Pass), 
            Rule.Of(StatementType.Type, 
                Production.Of(StatementType.ArrayType), 
                Parameters.Of(true), 
                Pass), 
            Rule.Of(StatementType.SimpleType, 
                Production.Of(StatementType.TypeId), 
                Parameters.Of(true), 
                SimpleType),
            Rule.Of(StatementType.ArrayType, 
                Production.Of(KeywordType.Array, TokenType.OpenBlock, StatementType.ArrayTypeCont), 
                Parameters.Of(false, false, true), 
                ArrayType),
            Rule.Of(StatementType.ArrayTypeCont, 
                Production.Of(StatementType.IntegerExpr, TokenType.CloseBlock, KeywordType.Of, StatementType.SimpleType), 
                Parameters.Of(true, false, false, true), 
                Pass),
            Rule.Of(StatementType.ArrayTypeCont, 
                Production.Of(TokenType.CloseBlock, KeywordType.Of, StatementType.SimpleType), 
                Parameters.Of(false, false, true), 
                Pass),
            Rule.Of(StatementType.Block, 
                Production.Of(KeywordType.Begin, StatementType.Statement, StatementType.StatementListBlockEnd), 
                Parameters.Of(false, true, true), BlockStatement),
            Rule.Of(StatementType.StatementListBlockEnd, 
                Production.Of(TokenType.Separator, StatementType.StatementOrBlockEnd), 
                Parameters.Of(false, true), 
                Pass),
            Rule.Of(StatementType.StatementListBlockEnd, 
                Production.Of(KeywordType.End), 
                Parameters.Of(false), 
                Pass),
            Rule.Of(StatementType.StatementOrBlockEnd, 
                Production.Of(StatementType.Statement, StatementType.StatementListBlockEnd), 
                Parameters.Of(true, true), 
                Pass), 
            Rule.Of(StatementType.StatementOrBlockEnd, 
                Production.Of(KeywordType.End), 
                Parameters.Of(false), 
                Pass),
            Rule.Of(StatementType.Statement, 
                Production.Of(StatementType.SimpleStatement), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.Statement, 
                Production.Of(StatementType.StructuredStatement), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.Statement, 
                Production.Of(StatementType.Declaration), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.SimpleStatement, 
                Production.Of(StatementType.AssignOrCallStatement),
                Parameters.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleStatement, 
                Production.Of(StatementType.ReturnStatement), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.SimpleStatement, 
                Production.Of(StatementType.AssertStatement), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.SimpleStatement, 
                Production.Of(StatementType.ReadStatement), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.SimpleStatement, 
                Production.Of(StatementType.WriteStatement), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.AssignOrCallStatement,
                Production.Of(StatementType.Identifier, StatementType.AssignmentStatementOrCall), 
                Parameters.Of(true, true), 
                AssignOrCallStatement),
            Rule.Of(StatementType.AssignmentStatementOrCall,
                Production.Of(TokenType.OpenBlock, StatementType.IntegerExpr, TokenType.CloseBlock, TokenType.Assignment, StatementType.Expr),
                Parameters.Of(false, true, false, false, true),
                ArrayAssignmentStatement),
            Rule.Of(StatementType.AssignmentStatementOrCall,
                Production.Of(TokenType.Assignment, StatementType.Expr),
                Parameters.Of(false, true),
                AssignmentStatement),
            Rule.Of(StatementType.AssignmentStatementOrCall, 
                Production.Of(TokenType.OpenParen, StatementType.Arguments, TokenType.CloseParen), 
                Parameters.Of(false, true, false), 
                CallStatement),
            Rule.Of(StatementType.Arguments, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.Arguments, 
                Production.Of(StatementType.Expr, StatementType.Exprs), 
                Parameters.Of(true, true), 
                Arguments),
            Rule.Of(StatementType.Exprs, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.Exprs, 
                Production.Of(TokenType.Comma, StatementType.Expr, StatementType.Exprs), 
                Parameters.Of(false, true, true), 
                Flatten),
            Rule.Of(StatementType.ReturnStatement, 
                Production.Of(KeywordType.Return, StatementType.OptReturnExpr), 
                Parameters.Of(false, true), null),
            Rule.Of(StatementType.OptReturnExpr, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.OptReturnExpr, 
                Production.Of(StatementType.Expr), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.ReadStatement, 
                Production.Of(KeywordType.Read, TokenType.OpenParen, StatementType.Variable, StatementType.Variables, TokenType.CloseParen), 
                Parameters.Of(false, false, true, true, false), null),
            Rule.Of(StatementType.Variables, Production.EpsilonProduction, Parameters.NoParameters, null),                 
            Rule.Of(StatementType.Variables, 
                Production.Of(TokenType.Comma, StatementType.Variable, StatementType.Variables), 
                Parameters.Of(false, true, true), 
                Flatten),                 
            Rule.Of(StatementType.WriteStatement, 
                Production.Of(KeywordType.WriteLn, TokenType.OpenParen, StatementType.Arguments, TokenType.CloseParen), 
                Parameters.Of(false, false, true, false), 
                null),
            Rule.Of(StatementType.AssertStatement, 
                Production.Of(KeywordType.Assert, TokenType.OpenParen, StatementType.BooleanExpr, TokenType.CloseParen), 
                Parameters.Of(false, false, true, false), 
                null),
            Rule.Of(StatementType.StructuredStatement, 
                Production.Of(StatementType.Block), 
                Parameters.Of(true), Pass),
            Rule.Of(StatementType.StructuredStatement, 
                Production.Of(StatementType.IfStatement), 
                Parameters.Of(true), Pass),
            Rule.Of(StatementType.StructuredStatement, 
                Production.Of(StatementType.WhileStatement), 
                Parameters.Of(true), Pass),
            Rule.Of(StatementType.IfStatement, 
                Production.Of(KeywordType.If, StatementType.BooleanExpr, KeywordType.Then, StatementType.Statement, StatementType.ElseBranch), 
                Parameters.Of(false, true, false, true, true), 
                IfStatement),
            Rule.Of(StatementType.ElseBranch, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.ElseBranch, 
                Production.Of(KeywordType.Else, StatementType.Statement), 
                Parameters.Of(false, true), 
                Pass),
            Rule.Of(StatementType.WhileStatement, 
                Production.Of(KeywordType.While, StatementType.BooleanExpr, KeywordType.Do, StatementType.Statement), 
                Parameters.Of(false, true, false, true), null),
            Rule.Of(StatementType.Expr, 
                Production.Of(StatementType.SimpleExpr, StatementType.ExprCont), 
                Parameters.Of(true, true), 
                Expr),
            Rule.Of(StatementType.ExprCont, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.ExprCont, 
                Production.Of(StatementType.RelationalOperator, StatementType.SimpleExpr), 
                Parameters.Of(true, true), 
                Pass),
            Rule.Of(StatementType.SignTerm,
                Production.Of(StatementType.Sign, StatementType.Term),
                Parameters.Of(true, true),
                SignTerm),
            Rule.Of(StatementType.SignTerm,
                Production.Of(StatementType.Term),
                Parameters.Of(true),
                Pass),
            Rule.Of(StatementType.SimpleExpr, 
                Production.Of(StatementType.SignTerm, StatementType.SimpleExprCont), 
                Parameters.Of(true, true), 
                SimpleExprOrTerm),
            Rule.Of(StatementType.SimpleExprCont, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.SimpleExprCont, 
                Production.Of(StatementType.AddingOperator, StatementType.Term, StatementType.SimpleExprCont), 
                Parameters.Of(true, true, true), 
                Flatten),
            Rule.Of(StatementType.Term, 
                Production.Of(StatementType.Factor, StatementType.TermCont), 
                Parameters.Of(true, true), 
                SimpleExprOrTerm),
            Rule.Of(StatementType.TermCont, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.TermCont, 
                Production.Of(StatementType.MultiplyingOperator, StatementType.Factor, StatementType.TermCont), 
                Parameters.Of(true, true, true), 
                Pass),
            Rule.Of(StatementType.IntegerExpr, 
                Production.Of(StatementType.Expr), 
                Parameters.Of(true), 
                Expr), // TODO?
            Rule.Of(StatementType.BooleanExpr, 
                Production.Of(StatementType.Expr), 
                Parameters.Of(true),
                Expr), // TODO?
            Rule.Of(StatementType.Factor, 
                Production.Of(StatementType.CallOrVariable, StatementType.FactorSize), 
                Parameters.Of(true, true), 
                FactorOptSize),
            Rule.Of(StatementType.Factor, 
                Production.Of(StatementType.Literal), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.Factor, 
                Production.Of(TokenType.OpenParen, StatementType.Expr, TokenType.CloseParen, StatementType.FactorSize), 
                Parameters.Of(false, true, false, true), 
                FactorOptSize),
            Rule.Of(StatementType.Factor, 
                Production.Of(KeywordType.Not, StatementType.Factor), 
                Parameters.Of(true, true), 
                Unary),
            Rule.Of(StatementType.FactorSize, Production.EpsilonProduction, Parameters.NoParameters, null),
            Rule.Of(StatementType.FactorSize, 
                Production.Of(TokenType.Dot, KeywordType.Size), 
                Parameters.Of(false, true), 
                Pass), // TODO: hmm
            Rule.Of(StatementType.CallOrVariable, 
                Production.Of(StatementType.Identifier, StatementType.CallOrVariableCont), 
                Parameters.Of(true, true), 
                CallOrVariable),
            Rule.Of(StatementType.CallOrVariableCont, 
                Production.Of(StatementType.VariableCont), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.CallOrVariableCont, 
                Production.Of(TokenType.OpenParen, StatementType.Arguments, TokenType.CloseParen), 
                Parameters.Of(false, true, false), 
                Flatten),
            Rule.Of(StatementType.Variable, 
                Production.Of(StatementType.Identifier, StatementType.VariableCont), 
                Parameters.Of(true, true), 
                Variable),
            Rule.Of(StatementType.VariableCont, 
                Production.EpsilonProduction, 
                Parameters.NoParameters, 
                null),
            Rule.Of(StatementType.VariableCont, 
                Production.Of(TokenType.OpenBlock, StatementType.IntegerExpr, TokenType.CloseBlock), 
                Parameters.Of(false, true, false), 
                Pass),
            Rule.Of(StatementType.RelationalOperator, 
                Production.Of("="), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.RelationalOperator, 
                Production.Of("<>"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.RelationalOperator, 
                Production.Of("<"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.RelationalOperator, 
                Production.Of("<="), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.RelationalOperator, 
                Production.Of(">="), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.RelationalOperator, 
                Production.Of(">"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.Sign, 
                Production.Of("+"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.Sign, 
                Production.Of("-"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.AddingOperator, 
                Production.Of("+"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.AddingOperator, 
                Production.Of("-"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.AddingOperator, 
                Production.Of(KeywordType.Or), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.MultiplyingOperator, 
                Production.Of("*"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.MultiplyingOperator, 
                Production.Of("/"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.MultiplyingOperator, 
                Production.Of("%"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.MultiplyingOperator, 
                Production.Of(KeywordType.And), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.Literal, 
                Production.Of(StatementType.NumberLiteral), 
                Parameters.Of(true), 
                Literal),
            Rule.Of(StatementType.Literal, 
                Production.Of(StatementType.StringLiteral), 
                Parameters.Of(true), 
                Literal),
            Rule.Of(StatementType.Literal, 
                Production.Of(StatementType.BooleanLiteral), 
                Parameters.Of(true), 
                Literal),
            Rule.Of(StatementType.NumberLiteral, 
                Production.Of(TokenType.IntegerValue), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.NumberLiteral, 
                Production.Of(TokenType.RealValue), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.StringLiteral, 
                Production.Of(TokenType.StringValue), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.BooleanLiteral, 
                Production.Of(TokenType.BooleanValue), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.TypeId, 
                Production.Of("integer"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.TypeId, 
                Production.Of("real"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.TypeId, 
                Production.Of("string"), 
                Parameters.Of(true), 
                Pass),
            Rule.Of(StatementType.TypeId, 
                Production.Of("boolean"), 
                Parameters.Of(true), 
                Pass)
        };

        static HashSet<dynamic> Terminals = new HashSet<dynamic>();
        static HashSet<dynamic> NonTerminals = new HashSet<dynamic>();
        static DefaultDictionary<dynamic, bool> Epsilons = new DefaultDictionary<dynamic, bool>();
        static DefaultDictionary<dynamic, HashSet<dynamic>> First = new DefaultDictionary<dynamic, HashSet<dynamic>>();
        static DefaultDictionary<dynamic, HashSet<dynamic>> Follow = new DefaultDictionary<dynamic, HashSet<dynamic>>();

        public static DefaultDictionary<dynamic, DefaultDictionary<dynamic, Rule>> Predictions =
            new DefaultDictionary<dynamic, DefaultDictionary<dynamic, Rule>>();

        static bool IsTerminal(dynamic i) => !(i is Production.Epsilon) && !IsNonTerminal(i);
        static bool IsNonTerminal(dynamic i) => !(i is Production.Epsilon) && NonTerminals.Contains(i);

        static IEnumerable<dynamic> FilterEpsilon(IEnumerable<dynamic> s)
        {
            return new HashSet<dynamic>(s.Where(i => !(i is Production.Epsilon)));
        }

        public static void CreateGrammar()
        {
            
            foreach (var rule in Rules)
            {
                NonTerminals.Add(rule.Name);
                foreach (var production in rule.Production.Items)
                {
                    if (!(production is StatementType)) Terminals.Add(production); // not necessary true?
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
            {
                foreach (var key2 in Predictions[key].Keys)
                {
                    Console.WriteLine($"{key}: {key2} -> {{{string.Join(", ", Predictions[key][key2].Production.Items)}}}");
                }
            }
        }

        private static void CalculateFirst()
        {
            foreach (var terminal in Terminals)
            {
                First[terminal].Add(terminal);
            }

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
                        {
                            rest.Select((i, idx) => (i, idx)).All(tuple =>
                            {
                                var (restItem, restIndex) = tuple;
                                if (IsNonTerminal(restItem))
                                {

                                    set.UnionWith(FilterEpsilon(First[restItem]));

                                    if (First[item].Contains(Production.Epsilon))
                                    {
                                        if (restIndex + 1 < rest.Count())
                                        {
                                            return true;
                                        }

                                        set.UnionWith(Follow[nonTerminal]);
                                    }
                                } else
                                {
                                    set.Add(restItem);
                                }

                                return false;
                            });
                        }
                        else
                        {
                            set.UnionWith(Follow[nonTerminal]);
                        }

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
                var firstItem = rule.Production[0];
                var nonTerminal = rule.Name;
                
                rule.Production.Items.Select((i, idx) => (i, idx)).All(tuple =>
                {
                    var (item, index) = tuple;

                    set.UnionWith(First[item]);
                    if (!Epsilons[item]) return false;

                    if (index + 1 < rule.Production.Items.Length)
                    {
                        set.UnionWith(First[rule.Production[index + 1]]);
                    }
                    else
                    {
                        set.UnionWith(Follow[item]);
                    }
                    return true; //false;
                });

                foreach (var s in set)
                {
                    if (Predictions[nonTerminal].ContainsKey(s))
                    {
                        Console.WriteLine($"ambiguous rule {nonTerminal} -> {s}");
                    }
                    Predictions[nonTerminal][s] = rule;
                }
            }
        }
        
    }
}