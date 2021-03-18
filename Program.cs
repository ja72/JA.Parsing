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
            Console.WriteLine("Defined Constants:");
            foreach (var (sym, val) in Expr.Constants)
            {
                Console.WriteLine($"{sym,5} = {val}");
            }
            Console.WriteLine();
            TestParse1();
            TestParse2();
            TestParse3();

#if DEBUG
            Console.WriteLine("Press ENTER to end.");
            Console.ReadLine();
#endif
        }

        static void TestParse1()
        {
            var pi = Expr.Const("pi");
            Console.WriteLine($"π={pi.Value}");

            var s_input = "x+y+z";
            var s_expr = Expr.Parse(s_input);
            Console.WriteLine(s_expr);
            Console.WriteLine($"vars={string.Join(",", s_expr.GetVariables().AsEnumerable())}");

            var f_input = "2.5*(1-exp(-pi*t))";
            var f_expr = Expr.Parse(f_input);
            Console.WriteLine(f_expr);
            Console.WriteLine($"{"t",-12} {"x",-12}");
            for (int i = 0; i < 10; i++)
            {
                var t = 0.125 * i-0.5;
                var x = f_expr.Eval(("t", t));
                Console.WriteLine($"{t,-12:g4} {x,-12:g4}");
            }
            Console.WriteLine();
        }

        static void TestParse2()
        {
            VariableExpr x = "x", y="y";

            var w_input = "x^2 + 2*x*y + x/y";
            var w = Expr.Parse(w_input);
            Console.WriteLine($"w={w}");
            Console.WriteLine($"vars={string.Join(",", w.GetVariables().AsEnumerable())}");
            var wx = w.Partial(x);
            Console.WriteLine($"wx={wx}");
            var wy = w.Partial(y);
            Console.WriteLine($"wy={wy}");
            var wp = w.TotalDerivative(x, y);
            Console.WriteLine($"wp={wp}");

            var f_input = "(x^2-pi)/(x^2+pi)";
            Console.WriteLine($"input={f_input}");
            var f = Expr.Parse(f_input);
            Console.WriteLine($"f={f}");
            Console.WriteLine($"f(0.5)={f.Eval(("x", 0.5))}");

            var df = f.Partial(x);
            Console.WriteLine($"df={df}");

            var fp = f.TotalDerivative();
            Console.WriteLine($"fp={fp}");

            Console.WriteLine();
        }
        static void TestParse3()
        {
            VariableExpr q = "q", r = "r";

            Expr x = r*Expr.Cos(q), y = r*Expr.Sin(q);
            Console.WriteLine($"pos = [{x},{y}]");
            Console.WriteLine($"vars={string.Join(",", x.GetVariables().AsEnumerable())}");
            Expr xp = x.TotalDerivative(), yp = y.TotalDerivative();
            Console.WriteLine($"vel = [{xp},{yp}]");
            Console.WriteLine($"vars={string.Join(",", xp.GetVariables().AsEnumerable())}");
            Expr xpp = xp.TotalDerivative(), ypp = yp.TotalDerivative();
            Console.WriteLine($"acc = [{xpp},{ypp}]");
            Console.WriteLine($"vars={string.Join(",", xpp.GetVariables().AsEnumerable())}");
        }
    }
}
