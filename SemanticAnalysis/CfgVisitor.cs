using System.Collections.Generic;
using Common;
using Common.AST;

namespace ScopeAnalyze
{
    public class CfgVisitor : AnalyzeVisitor
    {
        private static int _blockIdx = 0;
        private readonly List<Block> _blocks = new List<Block>();
        private readonly Stack<Block> _blockStack = new Stack<Block>();
        private readonly Stack<Block> _childBlockStack = new Stack<Block>();
        private readonly List<CFG> _result;

        private Block CurrentBlock => _blockStack.Count > 0 ? _blockStack.Peek() : null;
        private Block CurrentChildBlock => _childBlockStack.Count > 0 ? _childBlockStack.Peek() : null;
        
        public CfgVisitor(Scope scope, List<CFG> result) : base(scope)
        {
            _result = result;
            _blockStack.Push(null); //CreateBlock());
            _childBlockStack.Push(null);
        }

        private Block CreateBlock(List<Block> parents = null)
        {
            var block = new Block
            {
                Index = NextBlockId(),
                Parents = new List<Block>() // { CurrentBlock }
                //Parents = parents ?? new List<Block>()
            };

            _blocks.Add(block);

            return block;
        }

        private BranchBlock CreateBranchBlock(Node expression, Block trueBlock, Block falseBlock, List<Block> parents = null)
        {
            var block = new BranchBlock
            {
                Index = NextBlockId(),
                Expression = expression,
                TrueBlock = trueBlock,
                FalseBlock = falseBlock,
                Parents = new List<Block>() // { CurrentBlock }
                //Parents = parents ?? new List<Block>()
            };

            _blocks.Add(block);

            return block;
        }

        private static int NextBlockId() => _blockIdx++;

        private static IEnumerable<Node> FlattenBranchNode(BranchNode node)
        {
            var statements = new List<Node> {node.Left};

            var right = node.Right;
            if (right is BranchNode b)
            {
                statements.AddRange(FlattenBranchNode(b));
            }
            else
            {
                statements.Add(right);
            }

            return statements;
        }

        private void CheckBlockStack()
        {
            if (CurrentBlock == null) _blockStack.Push(CreateBlock());
        }
        
        public override dynamic Visit(ProgramNode node)
        {
            var mainVisitor = new CfgVisitor(CurrentScope, _result);
            mainVisitor.Visit((FunctionDeclarationNode) node.MainBlock);
            // _result.AddRange(mainVisitor._result);
            // node.MainBlock.Accept(this);

            node.DeclarationBlock.Accept(this);

            return _result;
        }

        public override dynamic Visit(NoOpNode node)
        {
            return node;
        }

        public override dynamic Visit(StatementListNode node)
        {
            CheckBlockStack();

            var statements = FlattenBranchNode(node);
            foreach (var statement in statements)
            {
                statement.Accept(this);
            }

            return null;
        }

        public override dynamic Visit(AssignmentNode node)
        {
            CurrentBlock.AddStatement(node);

            return null;
        }

        public override dynamic Visit(CallNode node)
        {
            CurrentBlock.AddStatement(node);

            return node;
        }

        public override dynamic Visit(BinaryOpNode node)
        {
            return null;
        }

        public override dynamic Visit(UnaryOpNode node)
        {
            return null;
        }

        public override dynamic Visit(ExpressionNode node)
        {
            return null;
        }

        public override dynamic Visit(SizeNode node)
        {
            return null;
        }

        public override dynamic Visit(IdentifierNode node)
        {
            return null;
        }

        public override dynamic Visit(LiteralNode node)
        {
            return null;
        }

        public override dynamic Visit(IfNode node)
        {
            var trueBlock = CreateBlock();
            Block falseBlock = null;
            if (!(node.FalseBranch is NoOpNode))
            {
                falseBlock = CreateBlock();
            }
            var afterBlock = CreateBlock();

            trueBlock.AddChild(afterBlock);
            falseBlock?.AddChild(afterBlock);
            afterBlock.AddChild(CurrentChildBlock);

            var ifBlock = CreateBranchBlock(node.Expression, trueBlock, falseBlock ?? afterBlock);

            CurrentBlock.AddBranch(ifBlock);

            _blockStack.Pop();
            _blockStack.Push(afterBlock);
            if (falseBlock != null) _blockStack.Push(falseBlock);
            _blockStack.Push(trueBlock);

            _childBlockStack.Push(afterBlock);

            node.TrueBranch.Accept(this);
            _blockStack.Pop();
            node.FalseBranch.Accept(this);
            _blockStack.Pop();

            _childBlockStack.Pop();
            
            return null;
        }

        public override dynamic Visit(WhileNode node)
        {
            var tempBlock = CreateBlock();
            var statementBlock = CreateBlock();
            var afterBlock = CreateBlock();

            CurrentBlock.AddChild(tempBlock);

            var branchBlock = CreateBranchBlock(node.Expression, statementBlock, afterBlock);
            tempBlock.Child = branchBlock;
            statementBlock.AddChild(tempBlock);

            afterBlock.AddChild(CurrentChildBlock);
            _childBlockStack.Push(tempBlock);

            _blockStack.Pop();
            _blockStack.Push(afterBlock);
            _blockStack.Push(statementBlock);

            
            node.Statement.Accept(this);
            _blockStack.Pop();
            _childBlockStack.Pop();

            return null;
        }

        public override dynamic Visit(VarDeclarationNode node)
        {
            CurrentBlock.AddStatement(node);

            return null;
        }

        public override dynamic Visit(FunctionDeclarationNode node)
        {
            var functionVisitor = new CfgVisitor(CurrentScope, _result);
            functionVisitor.Visit((StatementListNode) node.Statement);
            var cfg = new CFG
            {
                Blocks = functionVisitor._blocks,
                Function = node.Function
            };
            _result.Add(cfg);
            //node.Statement.Accept(this);

            return null;
        }

        public override dynamic Visit(ParameterNode node)
        {
            return null;
        }

        public override dynamic Visit(TypeNode node)
        {
            return null;
        }

        public override dynamic Visit(SimpleTypeNode node)
        {
            return null;
        }

        public override dynamic Visit(ArrayTypeNode node)
        {
            return null;
        }

        public override dynamic Visit(ReturnStatementNode node)
        {
            CurrentBlock.Child = null;

            CurrentBlock.AddStatement(node);
            CurrentBlock.Returned = true;
            
            return null;
        }

        public override dynamic Visit(AssertStatementNode node)
        {
            CurrentBlock.AddStatement(node);

            return null;
        }

        public override dynamic Visit(ReadStatementNode node)
        {
            CurrentBlock.AddStatement(node);

            return null;
        }

        public override dynamic Visit(WriteStatementNode node)
        {
            CurrentBlock.AddStatement(node);

            return null;
        }

        public override dynamic Visit(DeclarationListNode node)
        {
            var statements = FlattenBranchNode(node);
            foreach (var statement in statements)
            {
                statement.Accept(this);
            }

            return null;
        }

        public override dynamic Visit(VariableNode node)
        {
            return null;
        }

        public override dynamic Visit(ArrayDereferenceNode node)
        {
            return null;
        }

        public override dynamic Visit(ValueOfNode node)
        {
            return null;
        }

        public override dynamic Visit(ErrorNode node)
        {
            return null;
        }

        public override dynamic Visit(IntegerValueNode node)
        {
            return null;
        }

        public override dynamic Visit(RealValueNode node)
        {
            return null;
        }

        public override dynamic Visit(StringValueNode node)
        {
            return null;
        }

        public override dynamic Visit(BooleanValueNode node)
        {
            return null;
        }
    }
}