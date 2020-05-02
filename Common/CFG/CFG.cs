using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class CFG
    {
        public Function Function;
        public List<Block> Blocks;

        public override string ToString()
        {
            return $"{Function}\n{string.Join("\n", Blocks)}";
        }
    }
}