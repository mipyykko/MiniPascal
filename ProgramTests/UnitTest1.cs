using System;
using System.Collections.Generic;
using CodeGeneration;
using Common;
using Common.AST;
using NUnit.Framework;
using Parse;
using Scan;
using ScopeAnalyze;

namespace ProgramTests
{
    public class Tests
    {
        private static IEnumerable<TestCaseData> ProgramCases()
        {
            yield return new TestCaseData(
                @"program A;
begin
end.
"
            );
        }

        [TestCaseSource(nameof(ProgramCases))]
        public void ProgramTests(string source)
        {
            Context.Source = Text.Of(source);
            
            var parser = new Parser(new Scanner());
            var cfg = SemanticAnalyzer.Analyze((ProgramNode) parser.BuildTree());
            var generated = new Generator(cfg).Generate();
        }
    }
}