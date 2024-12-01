using Containers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Tests.Emission.DelegateConstruction.CodeGenerator
{
    public class Compiler
    {
        private static string StubString = @"

using System;
using System.Runtime;
using System.Collections.Generic;
using Containers.Models;
using Containers.Signals;
using Tests;
using Tests.Emission;
using Tests.Emission.DelegateConstruction;
using Tests.Emission.DelegateConstruction.CodeGenerator;

public class MethodHost : Model
{

    public MethodHost() : base(null) { }

%BODY%

}
";
        public static Assembly GenerateAssemblyFromMethods(string methods)
        {

            var asm = Assembly.Load("System.Runtime");


            string code = StubString.Replace("%BODY%", methods);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(
                "GeneratedMethods",
                syntaxTrees: new[] { syntaxTree },
                references: new[]
                {
                    MetadataReference.CreateFromFile(asm.Location),
                    MetadataReference.CreateFromFile(typeof(System.Runtime.DependentHandle).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Compiler).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Model).Assembly.Location)

                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return Assembly.Load(ms.ToArray());
                }
                else
                {
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Console.WriteLine(diagnostic);
                    }
                    throw new InvalidOperationException("Compilation failed.");
                }
            }
        }







    }



}
