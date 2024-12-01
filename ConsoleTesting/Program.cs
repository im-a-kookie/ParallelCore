// See https://aka.ms/new-console-template for more information
using Containers;
using Containers.Emission;
using Containers.Models;
using Containers.Threading.Pool;
using System.Reflection;



public class Thing
{

    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");


        Provider p = new Provider(new ParallelPoolSchema());


        p.ModelRegistry.Register(typeof(Model));


        Model m = new Model(p);

        p.StartProvider();

        p.StartModel(m);


        var exit = m.GetDelegate<string>("Exit");
        if (exit == null) throw new Exception("WAT");
        while (true)
        {
            var s = Console.ReadLine();
            if(s == "kill")
            {
                return;
            }
            else if (s == "Exit")
            {
                exit("Peanuts!");
            }
        }

    }




}

