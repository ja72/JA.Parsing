using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA
{
    using JA.Parsing;

    static class Program
    {
        static void Main(string[] args)
        {
            TestParse1();
            TestParse2();

#if DEBUG
            Console.WriteLine("Press ENTER to end.");
            Console.ReadLine();
#endif
        }

        static void TestParse1()
        {
            var input = "2.5*(1-exp(-pi*t))";
            var expr = Expr.Parse(input);
            Console.WriteLine(expr);
            Console.WriteLine($"{"t",-12} {"x",-12}");
            for (int i = 0; i < 10; i++)
            {
                var t = 0.125 * i-0.5;
                var x = expr.Eval(("t", t));
                Console.WriteLine($"{t,-12:g4} {x,-12:g4}");
            }
            Console.WriteLine();
        }

        static void TestParse2()
        {
            var input = "(x^2-1)/(x^2+1)";
            Console.WriteLine($"input={input}");
            var f = Expr.Parse(input);
            Console.WriteLine($"f(x)={f}");

            var df = f.Partial("x");
            Console.WriteLine($"df(x)/dx={df}");

            var fp = f.Derivative("x");
            Console.WriteLine($"df(x)/dt={fp}");

            Console.WriteLine();
        }
    }
}
