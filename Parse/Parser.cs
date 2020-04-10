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

        public bool MatchStack(dynamic a, Token b)
        {
            if (!(a.GetType() is Token) && !(a.GetType() is string)) return false;
            if (a.GetType() is string)
            {
                return a.Equals(b.Content);
            }
            if (a.GetType() is Token)
            {
                if (a.Type != b.Type) return false;
                if (a.Type == TokenType.Keyword && a.KeywordType != b.KeywordType) return false;
            }

            return true;
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
                    Console.WriteLine($"error: {Stack.Peek()} {_inputToken}");
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
            var top = Stack.Peek();

            if (!Predictions.ContainsKey(top))
            {
                throw new Exception($"no prediction exists for {top}");
            }

            if (!Predictions[top].ContainsKey(_inputToken)) 
            {
                if (!Predictions[top].ContainsKey(Production.Epsilon))
                {

                    throw new Exception($"no prediction for {_inputToken} in rule {top}");
                }

                return Predictions[top][Production.Epsilon];
            }

            return Predictions[top][_inputToken];
        }
        
        public void Gather(dynamic token)
        {
            if (!GathererStack.Any())
            {
                return;
            }

            GathererStack.Peek().Add(token);

            while (GathererStack.Peek().AllCollected)
            {
                var result = GathererStack.Pop().Result();

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