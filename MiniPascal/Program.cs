using System;
using Scan;
using Common;

namespace MiniPascal
{
    class Program
    {
        static void Main(string[] args)
        {
            var program = @"program MutualRecursion;
function F (n : integer) : integer;
begin
if n = 0 then
return 1;
else
return n - M (F (n-1));
end;
function M (n : integer) : integer;
begin
if n = 0 then return 0;
else return n - F (M (n-1));
end;
{* comment

still comment
*}
begin
var i : integer;
 i := 0;
while i <= 19 do writeln (F (i));
 i := 0;
while i <= 19 do writeln (M (i));
end. ";
            Context.Source = Text.Of(program);
            var s = new Scanner();
            while (!Context.Source.IsExhausted)
            {
                Console.WriteLine(s.GetNextToken());
            }
        }
    }
}