// See https://aka.ms/new-console-template for more information
using Containers.Addressing;
using Containers.Emission;
using Containers.Models;
using Containers.Signals;
using System.Text;

Console.WriteLine("Hello, World!");



Explorer.MapTypeArrays(Explorer.GetDelegateSignature(typeof(Router.EndpointCallback)).parameterTypes, new Type[] { typeof(object), typeof(object), typeof(object), typeof(Signal), typeof(StringBuilder), typeof(StringBuilder) });