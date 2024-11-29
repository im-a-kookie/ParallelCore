// See https://aka.ms/new-console-template for more information
using Containers;
using Containers.Emission;
using Containers.Models;
using System.Reflection;



public class Thing
{

    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");


        var t = new TestyThing();

        var m = typeof(TestyThing).GetMethod("TestCall", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)!;

        var d = DelegateBuilder.CreateCallbackDelegate(t, m);

        Test(new Datas());

        var result = d(null, new Datas(), t, null, null, null, null);

        if (result is Datas dt) Console.WriteLine("Result: " + dt.data);
        else Console.WriteLine("Result: " + (result ?? "<null>"));

    }


    static void Test(object? t)
    {
        Console.WriteLine(t);
        Console.WriteLine(t.GetType());
        Console.WriteLine(t.GetType().IsValueType);
    }


}


public struct Datas
{
    public int data;
    public double value;
}

public class TestyThing : Model
{
    int value = 123;

    public TestyThing(Provider provider) : base(provider)
    {
    }

    public TestyThing() : base(null)
    {

    }

    public static Datas? TestCall(TestyThing? me, Datas? x, Datas? y)
    {
        return new() { data = 10 };
    }

}


