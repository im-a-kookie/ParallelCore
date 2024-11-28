// See https://aka.ms/new-console-template for more information
using Containers;
using Containers.Addressing;
using Containers.Emission;
using Containers.Models;
using Containers.Signals;
using System.Reflection;
using System.Text;



public class Thing
{

    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");


        var t = new TestyThing(123);

        var m = typeof(TestyThing).GetMethod("TestCall", BindingFlags.Instance | BindingFlags.Public)!;

        var d = DelegateBuilder.CreateCallbackDelegate(t, m);

        d(null, null, t, null, null, null, null);

    }


}


public class TestyThing : Model
{

    public int MyData = 10;

    public TestyThing(Provider provider) : base(provider)
    {
    }


    public TestyThing(int x) : base(null) {

        MyData = x;
    }

    public void TestCall(TestyThing me)
    {
        Console.WriteLine($"{me.MyData}");
    }

}


