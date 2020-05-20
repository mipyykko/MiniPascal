using System.Collections.Generic;
using System.Linq;
using Common.AST;

namespace Common
{
    public class Block
    {
        public int Index;
        public List<Block> Parents;
        public Block Child;
        public List<Node> Statements = new List<Node>();
        public bool Returned;

        public void AddParent(Block b)
        {
            Parents.Add(b);
        }

        public void AddChild(Block b)
        {
            Child = b;
            b?.Parents.Add(this);
        }

        public void AddBranch(BranchBlock b)
        {
            AddChild(b);
            b?.TrueBlock.Parents.Append(this);
            b?.FalseBlock.Parents.Append(this);
        }

        public void AddStatement(Node n)
        {
            if (!Returned) Statements.Add(n);
        }
        public void AddStatements(IEnumerable<Node> l)
        {
            if (!Returned) Statements.AddRange(l);
        }

        public override string ToString()
        {
            return $"{Index}: {(Statements.Count > 0 ? string.Join("\n", Statements) : "(empty block)")}";
        }
    }        
    
    public class BranchBlock : Block
    {
        public Node Expression;
        public Block TrueBlock;
        public Block FalseBlock;
        public Block AfterBlock;
        
        public override string ToString()
        {
            return $"{Index}: {Expression}\n{TrueBlock} {FalseBlock}";
        }

    }
}