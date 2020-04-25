using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Common;
using Common.AST;
using Scan;
using Rule = Common.Rule;
using StatementType = Common.StatementType;

namespace Parse
{
    public class Parser
    {
        private Text Source => Context.Source;
        private readonly Scanner _scanner;
        private Token _inputToken;

        private void NextToken()
        {
            _inputToken = _scanner.GetNextToken();
        }

        private KeywordType InputTokenKeywordType => _inputToken.KeywordType;
        private TokenType InputTokenType => _inputToken.Type;
        private string InputTokenContent => _inputToken.Content;
        private static dynamic Predictions => Grammar.Predictions;

        private readonly Stack<dynamic> Stack = new Stack<dynamic>();
        private readonly Stack<Gatherer> GathererStack = new Stack<Gatherer>();

        private Node Program;

        public Parser(Scanner scanner)
        {
            Grammar.CreateGrammar();
            _scanner = scanner;
        }

        private static readonly StatementType[] Operators =
        {
            StatementType.AddingOperator,
            StatementType.MultiplyingOperator,
            StatementType.RelationalOperator
        };

        public static bool MatchStack(dynamic a, Token b)
        {
            if (!(a is Production.Epsilon) && a is string) return a.Equals(b.Content);

            return a switch
            {
                TokenType _ => (a == b.Type),
                KeywordType _ => (a == b.KeywordType),
                // ||
                // (st == StatementType.SimpleType && b.Type == TokenType.Identifier && types.Contains(b.Content)))
                StatementType st when Operators.Contains(st) && b.Type == TokenType.Operator => true,
                _ => false
            };
        }

        public Node BuildTree()
        {
            Stack.Push(StatementType.ProgramStatement);

            var error = false;
            
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

                    error = false;
                    continue;
                }

                Rule rule = null;

                error = true;
                var alreadyPutErrorToken = false;
                
                var errorStack = new Stack<Gatherer>();
                
                /*
                 * TODO:
                 *
                 * so as to remember what this is supposed to do!
                 *
                 * problems here:
                 *
                 * - it will jump way, way too far ahead - if there's an error in a
                 *   function declaration, the earliest it can find something is
                 *   another function declaration or the main block!
                 * - the error nodes aren't actually put into the tree!
                 */
                while (error)
                {
                    try
                    {
                        rule = Predict(Stack.Peek(), _inputToken);
                        error = false;
                    }
                    catch
                    {
                        /* - if we encounter an error in prediction:
                         * - we find out how many items the top-of-the-stack gatherer was
                         *   still waiting and pop them out of the stack
                         */

                        if (!alreadyPutErrorToken)
                        {
                            alreadyPutErrorToken = true;
                            error = true;
                            Console.WriteLine($"error: expected {Stack.Peek()}, got {_inputToken}");

                            var left = GathererStack.Peek().Error();
                            while (left-- > 0) Stack.Pop();

                            /* - we emit an error token to an error rule and gather it only once,
                             *   (hopefully) producing an error node
                             * - we go to the next token
                             */
                            rule = Grammar.ErrorRule;
                            _inputToken = Token.Of(
                                TokenType.Error,
                                KeywordType.Unknown,
                                InputTokenContent,
                                _inputToken.SourceInfo);
                            GathererStack.Push(rule.Gatherer);
                            Gather(_inputToken);
                            NextToken();
                        }

                        var found = false;
                        /* - we start popping out the gatherer stack, but storing the popped
                         *     to another
                        */
                        while (GathererStack.Any())
                        {
                            errorStack.Push(GathererStack.Pop());
                            try
                            {
                                /* - after each pop, we check if the next item awaited in top gatherer
                                 *   is an ok rule with the current input token
                                 */
                                Predict(GathererStack.Peek().Next, _inputToken);
                                Stack.Push(GathererStack.Peek().Next);
                                errorStack.Clear();
                                found = true;
                                break;
                                // - if so, we continue to normal prediction and continue there
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        if (!found)
                        {
                            /* - if we run out of the gatherer stack, we push the popped gatherers
                             *   back in, get the next token and try again, until we find something
                             *   to continue from
                             */
                            NextToken();
                            while (errorStack.Any()) GathererStack.Push(errorStack.Pop());
                        }

                        //break;
                    }
                }

                if (rule.Gatherer != null)
                    GathererStack.Push(rule.Gatherer);
                else if (!(rule.Production[0] is Production.Epsilon)) throw new Exception($"null gatherer for {rule}");

                Stack.Pop();

                foreach (var prod in rule.Production.Items.Reverse()) Stack.Push(prod);
            }

            return Program;
        }

        public Rule Predict(dynamic top, Token token)
        {
            //var top = Stack.Peek();

            var toMatch = token.Type switch // was: InputTokenType
            {
                TokenType.Keyword => (dynamic) token.KeywordType, // was: InputTokenKeywordType
                TokenType.Operator => token.Content, // InputTokenContent,
                TokenType.Identifier when (top == StatementType.Type || top == StatementType.SimpleType ||
                                           top == StatementType.TypeId) => token.Content, //InputTokenContent,
                _ => token.Type// InputTokenType
            };

            if (!Predictions.ContainsKey(top)) throw new Exception($"Grammar error: no prediction exists for {top}");

            if (!Predictions[top].ContainsKey(toMatch))
            {
                if (!Predictions[top].ContainsKey(Production.Epsilon))
                     throw new Exception($"no prediction for {toMatch} in rule {top}");

                Console.WriteLine($"--- ok, matching epsilon for {toMatch}");
                return Predictions[top][Production.Epsilon];
            }

            Console.WriteLine($"--- ok, found rule {top} for {toMatch}");
            return Predictions[top][toMatch];
        }

        public void Gather(dynamic token)
        {
            if (!GathererStack.Any()) return;

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

                if (result is ErrorNode || result is Array a && ((dynamic[]) a).Any(i => i is ErrorNode))
                {
                    GathererStack.Peek().Error();
                }
                else
                {
                    GathererStack.Peek().Add(result);
                }

            }
        }
    }
}