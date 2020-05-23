using System;
using System.Collections.Generic;
using CodeGeneration;
using Scan;
using Common;
using Common.AST;
using Common.Errors;
using Parse;
using ScopeAnalyze;

namespace MiniPascal
{
    internal class Program
    {
        private static void Main(string[] args)
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
while i <= 19 do 
begin
 writeln (F (i));
 i := i + 1
end;
 i := 0;
while i <= 19 do 
begin
writeln (M (i));
i := i + 1
end;
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
 Swap (A[0], A [1]); 
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

            var program4 = @"program GCD;
begin
var i, j : integer;
 read (i, j);
while i <> j do
  if (i > j) then 
    i := i - j
  else 
    j := j - i;
 writeln (i);
end.";

            var program5 = @"program StringArray;

begin
  var A : array[3] of string;
  var i : integer;
  var c : string;

  while i < A.size do
  begin
    writeln(""string"", i);
    read(A[i]);
    i := i + 1;
  end;
  
  i := 0;

  while i < A.size do
  begin
    writeln(A[i]);
    i := i + 1
  end;
end.";

            var program6 = @"program Recursive;

procedure A(depth : integer);
begin
  writeln(""A"", depth);
  A(depth + 2);
  if depth > 0 then 
    B(depth - 1);
end;

procedure B(depth : integer);
begin
  writeln(""B"", depth);
  if depth > 0 then A(depth - 1);
end;

begin
  A(50);
end.
";

            var program7 = @"program ArrayReturn;

function reverse(data : array[] of integer): array[] of integer;

begin
  var i : integer;
  i := 0;
  var B : array[data.size] of integer;

  while i < data.size do
  begin
    B[(data.size - 1) - i] := data[i];
    i := i + 1;
  end;

  return B;
end;

begin
  var A : array[100] of integer;

  var i : integer;
  i := 0;

  while i < A.size do
  begin
    A[i] := i * 2;
    i := i + 1;
  end;

  var B : array[A.size] of integer;
  B := reverse(A);

  i := 0;

  while i < B.size do 
  begin
    writeln(B[i]);
    i := i + 1;
  end
  
end.
";
            var program8 = @"program BoolArray;

begin
  var A : array[] of boolean;
  var B : array[] of integer;
  var C : array[10] of integer;
  var D : integer;
  D := 2 * (1 + 3);
  C[D] := 2;
  A[B[C[D]]] := true;
  read(B[0], C[0]);
  if (D < 6) or (C[D] > 1) then
    writeln(""yes"")
end.";
            var program9 = @"program A;
begin
  var a : string;
  a := +""kissa"";
end.
";
            Context.ErrorService = new ErrorService();
            Context.Source = Text.Of(program9);
        
            var s = new Scanner();
            var p = new Parser(s);

            var v = (ProgramNode) p.BuildTree();
            
            if (v == null || Context.ErrorService.HasErrors())
            {
              Context.ErrorService.Throw();
            }
            /*
             * TODO:
             * - check array sizes where the expression is possible to evaluate
             * - check for array type compatibility where array size is known      
             */
            var cfg = SemanticAnalyzer.Analyze(v);

            if (Context.ErrorService.HasErrors())
            {
              Context.ErrorService.Throw();
            }

            Console.WriteLine(v.AST());

            /*Console.WriteLine(JsonConvert.SerializeObject(v, Formatting.Indented, new JsonSerializerSettings
            {
              ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));*/
            Console.WriteLine(new Generator(cfg).Generate());
            // Console.WriteLine(v.AST());
        }
    }
}