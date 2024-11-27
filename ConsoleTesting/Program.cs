// See https://aka.ms/new-console-template for more information
using Containers.Addressing;
using Containers.Emission;
using Containers.Models;
using Containers.Signals;

Console.WriteLine("Hello, World!");


//generate a lot of strings
//If no collisions occur in 1 million iterations, then the model is definitely good enough
int iterations = 1023;

HashSet<string> included = new HashSet<string>(iterations);
Address<short> testAddress = new(123);

//bit density
int bitLen = (testAddress.Size) * 5;


var t = Explorer.GetDelegateSignature(typeof(Router.EndpointCallback)).parameterTypes;

Random r = new Random();

for(int i = 0; i < 5; i++)
{
    List<Type> tt = new();
    for(int j = 0; j < t.Length; ++j)
    {
        if (r.NextDouble() < 0.7) tt.Add(t[j]);
    }
    tt.Add(typeof(Packet));

    var ttt = tt.ToArray();
    r.Shuffle(ttt);

    Explorer.GenerateParameterMappings(t, ttt);

}

