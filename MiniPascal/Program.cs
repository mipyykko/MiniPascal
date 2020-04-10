using System;
using Scan;
using Common;
using Parse;

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
return 1
else
return n - M (F (n-1));
end;
function M (n : integer) : integer;
begin
if n = 0 then return 0
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
            var program2 = @"program SwapAndSumThem;
function Sum (data : array [] of integer) : integer;
begin
var i, sum : integer;
 i := 0; sum := 0;
while i < data.size do begin
 sum := sum + data [i]; i := i + 1;
end;
return sum;
end;
procedure Swap (var i : integer, var j : integer);
begin
var tmp : integer;
 tmp := i; i := j; j := tmp;
end;
begin
var A : array [2] of integer;
 read (A [0], A [1]);
 Swap (A [0], A [1]); 
 writeln (A [0], A [1]);
 writeln (""Sum is "", sum (A));
end.";

            var program3 = @"program A;
begin
if (1 < 2) then
  if (2 < 3) then
    writeln(""jes"")
  else
   if (3 < 4) then
     writeln(""nou"")
   else 
     writeln(""fwoop"")
  else
   writeln(""noh"");
end.";
  
            Context.Source = Text.Of(program2);

            var s = new Scanner();
            var p = new Parser(s);

            var v = p.BuildTree();
            Console.WriteLine(v.AST());
        }
    }
}