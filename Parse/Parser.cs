using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AST;
using Common;
using Scan;
using Common;
using Rule = Common.Rule;
using StatementType = Common.StatementType;

namespace Parse
{
    public class Parser
    {
        private Text Source => Context.Source;
        private readonly Scanner _scanner;
        private Token _inputToken;

        private void NextToken() => _inputToken = _scanner.GetNextToken();
        private KeywordType InputTokenKeywordType => _inputToken.KeywordType;
        private TokenType InputTokenType => _inputToken.Type;
        private string InputTokenContent => _inputToken.Content;
        private static dynamic Predictions => Grammar.Predictions;
        
        private static Node NoOpStatement = new NoOpNode();

        private Stack<dynamic> Stack = new Stack<dynamic>();
        private Stack<Gatherer> GathererStack = new Stack<Gatherer>();

        private Node Program;
        
        public Parser(Scanner scanner)
        {
            Grammar.CreateGrammar();
            _scanner = scanner;
        }

        private static StatementType[] operators =
        {
            StatementType.AddingOperator,
            StatementType.MultiplyingOperator,
            StatementType.RelationalOperator
        };
        
        public bool MatchStack(dynamic a, Token b)
        {
            if (!(a is Production.Epsilon) && a is string)
            {
                return a.Equals(b.Content);
            }
            if (a is TokenType)
            {
                return a == b.Type;
            }

            if (a is KeywordType)
            {
                return a == b.KeywordType;
            }

            if (a is StatementType && operators.Contains((StatementType) a) && b.Type == TokenType.Operator)
            {
                return true;
            }

            return false; 
        }
        
        public Node BuildTree()
        {
            Stack.Push(StatementType.ProgramStatement);

            NextToken();

            while (Stack.Any())
            {
                while (Stack.Peek() is Production.Epsilon)
                {
                    Stack.Pop();
                    Gather(null);
                }

                if (MatchStack(Stack.Peek(), _inputToken))
                {
                    Stack.Pop();
                    Gather(_inputToken);
                    NextToken();
                    continue;
                }

                Rule rule;

                try
                {
                    rule = Predict();
                }
                catch
                {
                    Console.WriteLine($"error: expected {Stack.Peek()}, got {_inputToken}");
                    break;
                }

                if (rule.Gatherer != null)
                {
                    GathererStack.Push(rule.Gatherer);
                }
                else if (!(rule.Production[0] is Production.Epsilon))
                {
                    throw new Exception($"null gatherer for {rule}");
                }

                Stack.Pop();

                foreach (var prod in rule.Production.Items.Reverse())
                {
                    Stack.Push(prod);
                }

            }

            return Program;
        }

        public Rule Predict()
        {
            Console.WriteLine($"--- predict? {Stack.Peek()}, got {_inputToken}");
            
            var top = Stack.Peek();

            dynamic toMatch = _inputToken.Type == TokenType.Keyword
                ? (dynamic) _inputToken.KeywordType
                : (dynamic) _inputToken.Type == TokenType.Operator
                    ? (dynamic) _inputToken.Content
                    : _inputToken.Type;
            
            if (!Predictions.ContainsKey(top))
            {
                throw new Exception($"no prediction exists for {top}");
            }

            if (!Predictions[top].ContainsKey(toMatch)) 
            {
                if (!Predictions[top].ContainsKey(Production.Epsilon))
                {

                    throw new Exception($"no prediction for {toMatch} in rule {top}");
                }

                Console.WriteLine($"--- ok, matching epsilon for {toMatch}");
                return Predictions[top][Production.Epsilon];
            }

            Console.WriteLine($"--- ok, found rule for {toMatch}");
            return Predictions[top][toMatch];
        }
        
        public void Gather(dynamic token)
        {
            if (!GathererStack.Any())
            {
                return;
            }

            if (GathererStack.Peek() == null) return;

            GathererStack.Peek().Add(token);

            while (GathererStack.Peek().AllCollected)
            {
                var result = GathererStack.Pop().Result;

                if (!GathererStack.Any())
                {
                    Program = result;
                    return;
                }

                GathererStack.Peek().Add(result);
            }
        }
        
     }
}