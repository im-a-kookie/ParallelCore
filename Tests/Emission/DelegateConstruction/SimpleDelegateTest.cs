using Containers.Emission;
using Containers.Models;
using Containers.Signals;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Emission.DelegateConstruction.CodeGenerator;

namespace Tests.Emission.DelegateConstruction
{
    [TestClass]
    public class TestDelegateBuilder
    {

        [TestMethod]
        public void TestSomething()
        {
            var s = MethodGen.GenerateMethodSignatures(new Random(12351));
            var str = string.Join("\n", s);
            Debug.WriteLine(str);
            var asm = Compiler.GenerateAssemblyFromMethods(str);
            Debug.WriteLine(asm.GetName());
            List<Router.EndpointCallback> delegates = new();
            int n = 0;
            int total = 0;
            foreach(var type in asm.GetTypes().Where(x => x.IsAssignableTo(typeof(Model))))
            {
                var c = type.GetConstructor(Type.EmptyTypes);
                var model = (Model)c!.Invoke(null);
                foreach(var method in type.GetMethods())
                {
                    // press F to pay respects
                    var d = DelegateBuilder.CreateCallbackDelegate(method);
                    if (method.ReturnType != typeof(void))
                    {
                        try
                        {
                            var result = d(null, null, model, null, null, null, "banana")!;
                            if (result is SampleDataClass sdc) n += sdc.value;
                            else if (result is SampleDataStruct sds) n += sds.value;
                            else if (result is int i) n += i;
                            else if (result is short sh) n += sh;
                            else if (result is byte b) n += b;
                            else if (result is long l) n += (int)l;
                            else if (result is float f) n += (int)f;
                            else if (result is double doub) n += (int)doub;
                        }
                        catch
                        {
                            Debug.WriteLine($"Failure, Method: {method.Name}. Definition: {s.Where(x => x.Contains(method.Name + "(")).FirstOrDefault()}");
                        }

                    }
                    ++total;
                }



            }

            Debug.WriteLine("Result: " + n);



        }

    }
}
