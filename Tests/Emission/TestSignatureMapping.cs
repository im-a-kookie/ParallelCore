using Containers.Emission;
using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Emission
{
    [TestClass]
    public class TestSignatureMapping
    {

        delegate int SampleDelegate(double first, StringBuilder second);

        [TestMethod]
        public void Test_GetDelegateSignature()
        {
            var t = typeof(SampleDelegate);

            var result = Explorer.GetDelegateSignature(t);

            Assert.AreEqual(result.returnType, typeof(int), "The return parameter was not correctly retrieved");
            Assert.AreEqual(result.parameterTypes[0], typeof(double), "The method signature was not correctly retrieved");
            Assert.AreEqual(result.parameterTypes[1], typeof(StringBuilder), "The method signature was not correctly retrieved");

        }

        [TestMethod]
        public void Test_SignatureMapping()
        {
            // Retrieve the router endpoint callback
            // At the end of the day, as long as our functions work for this delegate
            // Then problems with them don't really matter
            var inputTypes = Explorer.GetDelegateSignature(typeof(Router.EndpointCallback)).parameterTypes;


            var outputTypes = new Type[2] { typeof(StringBuilder), typeof(double) };

            var m = Explorer.GenerateParameterMappings(inputTypes, outputTypes);


        }


    }
}
