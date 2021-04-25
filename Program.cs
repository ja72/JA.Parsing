using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA
{
    using System.Diagnostics;
    using JA.Parsing;

    static class Program
    {
        static void Main(string[] args)
        {
            TestNamedConstants();
            TestUnaryFunctions();
            TestBinaryFunctions();
            TestParse1();
            TestParse2();
            TestParse3();
            TestArrayParse1();            
            TestArrayParse2();

#if DEBUG
            Console.WriteLine("Press ENTER to end.");
            Console.ReadLine();
#endif
        }

        static void TestArrayParse1()
        {
            Console.WriteLine("Vectorizing Array Parsing:");
            var input = @"[x^3,6*x^2*(1-x),6*x*(1-x)^2,(1-x)^3]/6";
            Console.WriteLine($"Input: {input}");
            var expr = Expr.Parse(input);
            Console.WriteLine($"y={expr}");
            var f_expr = expr.GetArray("x");
            const int colWidth = 12;
            Console.Write($"{"x",-colWidth} ");
            for (int j = 0; j < expr.ResultCount; j++)
            {
                var str = $"y[{ j}]";
                Console.Write($"{str,-colWidth} ");
            }
            Console.WriteLine();
            const int n = 12;
            for (int i = 0; i <= n; i++)
            {
                double x = (double)i/n;
                Console.Write($"{x,-colWidth:g4} ");
                double[] y = f_expr(x);
                for (int j = 0; j < y.Length; j++)
                {
                    Console.Write($"{y[j],-colWidth:g4} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        static void TestArrayParse2()
        {
            Console.WriteLine("Mismatch Array Parsing:");
            var input = @"[x, 1-x] + [1,2,3]";
            Console.WriteLine($"Input: {input}");
            var expr = Expr.Parse(input);
            Console.WriteLine($"y={expr}");
            var f_expr = expr.GetArray("x");
            const int colWidth = 12;
            Console.Write($"{"x",-colWidth} ");
            for (int j = 0; j < expr.ResultCount; j++)
            {
                var str = $"y[{ j}]";
                Console.Write($"{str,-colWidth} ");
            }
            Console.WriteLine();
            const int n = 4;
            for (int i = 0; i <= n; i++)
            {
                double x = (double)i/n;
                Console.Write($"{x,-colWidth:g4} ");
                double[] y = f_expr(x);
                for (int j = 0; j < y.Length; j++)
                {
                    Console.Write($"{y[j],-colWidth:g4} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void TestNamedConstants()
        {
            Console.WriteLine("Named Constants:");
            foreach (var (sym, val) in Expr.Constants)
            {
                Console.WriteLine($"{sym,5} = {val}");
            }
            Console.WriteLine();

            Console.WriteLine("Test Substitutions:");

            Expr expr = "(2*x+1)/(y^2+1)";
            Console.WriteLine($"f={expr}");

            Console.Write($"Substitute: x=>10, ");
            Expr sub = expr.Substitute("x", 10);
            Console.WriteLine($"f={sub}");
            Console.Write($"Substitute: x=>z/2, ");
            sub = expr.Substitute("x", "z/2");
            Console.WriteLine($"f={sub}");            
            Console.Write($"Substitute: x=>0.5*y-1, ");
            sub = expr.Substitute("x", "0.5*y-1");
            Console.WriteLine($"f={sub}");
            Console.Write($"Substitute: y=>sqrt(x), ");
            sub = expr.Substitute("y", "sqrt(x)");
            Console.WriteLine($"f={sub}");
            Console.WriteLine();
        }

        static void TestUnaryFunctions()
        {
            Console.WriteLine("Test Unary Functions:");
            VariableExpr a = "a";
            double x = 2/Math.PI;
            string fstr = $"f({x})", fpstr = $"f'({x})";
            Console.WriteLine($"{"f",-12}  {"f'",-28} {fstr,-22} {fpstr,-28}");
            foreach (var op in Enum.GetValues(typeof(UnaryOp)) as UnaryOp[])
            {
                if (op==UnaryOp.Undefined) continue;
                var f_expr = Expr.Unary(op, a);
                var dfda = f_expr.Partial(a);
                var f = f_expr[a];
                var fp = dfda[a];

                Console.WriteLine($"{f_expr,-12} {dfda,-28:g4} {f(x),-22:g4} {fp(x),-28:g4}");
            }
            Console.WriteLine();
        }
        static void TestBinaryFunctions()
        {
            Console.WriteLine("Test Binary Functions:");
            VariableExpr a = "a", b = "b";
            double x = 5, y = 2;
            string fstr = $"f({x},{y})", fpstr = $"f'({x},{y})";
            Console.WriteLine($"{"f",-12} {"f'",-28} {fstr,-22} {fpstr,-28}");
            foreach (var op in Enum.GetValues(typeof(BinaryOp)) as BinaryOp[])
            {
                if (op==BinaryOp.Undefined) continue;
                var f_expr = Expr.Binary(op, a, b);
                var dfda = f_expr.Partial(a);
                var f = f_expr[a, b];
                var fp = dfda[a, b];                
                Console.WriteLine($"{f_expr,-12} {dfda,-28:g4} {f(x, y),-22:g4} {fp(x, y),-28:g4}");
            }
            Console.WriteLine();
        }

        static void TestParse1()
        {
            Console.WriteLine("Test Parsing Strings");
            var pi = Expr.Const("pi");
            Console.WriteLine($"π={pi.Value}");

            var s_input = "x+y+z";
            Console.WriteLine($"Input: {s_input}");
            var s_expr = Expr.Parse(s_input);
            Console.WriteLine($"Expr: {s_expr}");
            Console.WriteLine($"vars={string.Join(",", s_expr.GetVariables().AsEnumerable())}");
            
            var f_input = "2.5*(1-exp(-π*t))";
            Console.WriteLine($"Input: {f_input}");
            var f_expr = Expr.Parse(f_input);
            var f_fun = f_expr.GetFunction("t");
            Console.WriteLine($"x={f_expr}");
            Console.WriteLine($"{"t",-12} {"x",-12}");
            for (int i = 0; i < 10; i++)
            {
                var t = 0.125 * i-0.5;
                var x = f_fun(t);
                Console.WriteLine($"{t,-12:g4} {x,-12:g4}");
            }
            Console.WriteLine();
        }

        static void TestParse2()
        {
            Console.WriteLine("Test Multivariate Calculus:");

            VariableExpr x = "x", y="y";

            var w_input = "x^2 + 2*x*y + x/y";
            Console.WriteLine($"Input: {w_input}");
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
            Console.WriteLine($"Input: {f_input}");
            var f = Expr.Parse(f_input);
            Console.WriteLine($"f={f}");
            var fx = f["x"];
            Console.WriteLine($"f(0.5)={fx(0.5)}");

            var df = f.Partial(x);
            Console.WriteLine($"df={df}");

            var fp = f.TotalDerivative();
            Console.WriteLine($"fp={fp}");

            Console.WriteLine();
        }
        static void TestParse3()
        {
            Console.WriteLine("Test Kinematics:");
            VariableExpr q = "q", r = "r";
            VariableExpr qp = q.Rate(), rp = r.Rate();
            VariableExpr qpp = qp.Rate(), rpp = rp.Rate();

            Console.WriteLine($"Variables: {string.Join(", ", q, r)}");
            Console.WriteLine($"Rates: {string.Join(", ", qp, rp)}");
            Console.WriteLine($"Accelerations: {string.Join(", ", qpp, rpp)}");

            Expr pos = Expr.FromArray(r*Expr.Sin(q), -r*Expr.Cos(q));
            Console.WriteLine($"pos = {pos}");

            Expr vel = pos.TotalDerivative();
            Console.WriteLine($"vel = {vel}");

            Expr J = vel.Jacobian(rp,qp);
            Console.WriteLine($"jacobian = {J}");

            Expr acc = vel.TotalDerivative();
            Console.WriteLine($"acc = {acc}");

            Console.WriteLine();
        }
    }
}
