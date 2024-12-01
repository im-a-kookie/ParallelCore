// See https://aka.ms/new-console-template for more information
using Containers.Models;
using Containers.Threading;
using Containers.Threading.Pool;



public class Thing
{

    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");


        ParallelSchema p = new ParallelPoolSchema();

        Model m = new Model(p);

        m.OnModelEnd += (m) => Console.WriteLine($"Model 0x{m.Address} Exited!");


        var exit = m.GetDelegate<string>("Exit");
        if (exit == null) throw new Exception("WAT");
        while (true)
        {
            var s = Console.ReadLine();
            if (s == "kill")
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

