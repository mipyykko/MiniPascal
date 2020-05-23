using System;
using System.Collections;
using System.Collections.Generic;
using Common;
using Common.AST;
using NUnit.Framework;
using Parse;
using FluentAssertions;
using Moq;

namespace Test
{
    public class Tests
    {
        private static IEnumerable<TestCaseData> ProgramCases()
        {
            yield return new TestCaseData(typeof(IdentifierNode), typeof(NoOpNode),
                typeof(StatementListNode), new dynamic[]
                {
                    new IdentifierNode
                    {
                        Token = new Mock<Token>().Object
                    },
                    new StatementListNode(),
                    null,
                });
            yield return new TestCaseData(typeof(IdentifierNode), typeof(DeclarationListNode),
                typeof(StatementListNode), new dynamic[]
                {
                    new IdentifierNode
                    {
                        Token = new Mock<Token>().Object
                    },
                    new DeclarationListNode(),
                    new StatementListNode(),
                });
        }

        [TestCaseSource(nameof(ProgramCases))]
        public void ProgramTests(Type id, Type declaration, Type main, dynamic[] p)
        {
            var node = ParseTree.Program(p);
            ((Node) node).Should().BeOfType(typeof(ProgramNode));
            node = (ProgramNode) node;
            Assert.IsInstanceOf(id, node.Id);
            Assert.IsInstanceOf(declaration, node.DeclarationBlock);
            Assert.IsInstanceOf(main, node.MainBlock);
        }

        private static IEnumerable<TestCaseData> DeclarationBlockCases()
        {
            yield return new TestCaseData(typeof(NoOpNode), typeof(NoOpNode), new dynamic[] { });
            yield return new TestCaseData(typeof(VarDeclarationNode), typeof(NoOpNode), new dynamic[]
            {
                new VarDeclarationNode
                {
                    Ids = new List<Node>()
                },
                null
            });
            yield return new TestCaseData(typeof(VarDeclarationNode), typeof(NoOpNode), new dynamic[]
            {
                new VarDeclarationNode
                {
                    Ids = new List<Node>()
                },
                new NoOpNode()
            });
            yield return new TestCaseData(typeof(VarDeclarationNode), typeof(AssignmentNode), new dynamic[]
            {
                new VarDeclarationNode
                {
                    Ids = new List<Node>()
                },
                new AssignmentNode()
            });
        }

        [TestCaseSource(nameof(DeclarationBlockCases))]
        public void DeclarationBlockTests(Type a, Type b, dynamic[] p)
        {
            var node = (DeclarationListNode) ParseTree.DeclarationBlockStatement(p ?? new dynamic[] { });
            node.Left.Should().BeOfType(a);
            node.Right.Should().BeOfType(b);
        }

        [Test]
        public void AssignOrCallStatementTests()
        {
            var id = new IdentifierNode();
            var assignment = new AssignmentNode
            {
                LValue = new VariableNode()
            };
            var node = (AssignmentNode) ParseTree.AssignOrCallStatement(new dynamic[] {id, assignment});
            node.Should().Equals(assignment);
            node.LValue.Id.Should().Equals(id);

            var call = new CallNode();
            var node2 = (CallNode) ParseTree.AssignOrCallStatement(new dynamic[] {id, call});
            node2.Should().Equals(call);
            node2.Id.Should().Equals(id);
        }

        private static IEnumerable<TestCaseData> CallOrVariableCases()
        {
            yield return new TestCaseData(typeof(CallNode), new dynamic[]
            {
                new IdentifierNode
                {
                    Token = new Mock<Token>().Object
                },
                new ParseTree.TreeNode
                {
                    Left = new Mock<Node>().Object,
                    Right = new ParseTree.TreeNode
                    {
                        Left = new Mock<Node>().Object,
                        Right = null
                    }
                }
            });
            yield return new TestCaseData(typeof(VariableNode), new dynamic[]
            {
                new IdentifierNode
                {
                    Token = new Mock<Token>().Object
                },
                null
            });
            yield return new TestCaseData(typeof(ArrayDereferenceNode), new dynamic[]
            {
                new IdentifierNode
                {
                    Token = new Mock<Token>().Object
                },
                new IntegerValueNode
                {
                    Token = new Mock<Token>().Object,
                    Value = 0
                }
            });
        }

        [TestCaseSource(nameof(CallOrVariableCases))]
        public void CallOrVariableTests(Type t, dynamic[] p)
        {
            var node = ParseTree.CallOrVariable(p);
            node.Should().BeOfType(t);
            ((IdNode) node).Id.Should().Equals(p[0]);
            node.Token.Should().Equals(((IdentifierNode) p[0]).Token);
        }
    }
}