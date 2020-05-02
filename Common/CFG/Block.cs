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
        public Node Node;

        public void AddParent(Block b)
        {
            Parents.Add(b);
            //Parent = b;
            // foreach (var parent in Parents) parent.Children.Add(this);
        }

        public void AddChild(Block b)
        {
            Child = b;
            b?.Parents.Add(this);
            // Children.Add(b);
        }

        public void AddBranch(BranchBlock b)
        {
            AddChild(b);
            b?.TrueBlock.Parents.Append(this);
            b?.FalseBlock.Parents.Append(this);
        }
        
        public void AddStatement(Node n) => Statements.Add(n);
        public void AddStatements(IEnumerable<Node> l) => Statements.AddRange(l);

        public override string ToString()
        {
            return $"{string.Join("\n", Statements)}";
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
            return "";//$"{string.Join("\n", Statements)}\n{TrueBlock}-{FalseBlock}";
        }

    }
}