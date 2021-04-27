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
            TestCubicSpline();

#if DEBUG
            Console.WriteLine("Press ENTER to end.");
            Console.ReadLine();
#endif
        }

        static void TestNamedConstants()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Named Constants:");
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach (var (sym, val) in Expr.Constants)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{sym,5}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" = ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{val}");
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            Console.WriteLine("Test Substitutions:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Expr expr = "(2*x+1)/(y^2+1)";
            Console.WriteLine($"f={expr}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"Substitute: x=>10, ");
            Expr sub = expr.Substitute("x", 10);
            Console.WriteLine($"f={sub}");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write($"Substitute: x=>z/2, ");
            sub = expr.Substitute("x", "z/2");
            Console.WriteLine($"f={sub}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"Substitute: x=>0.5*y-1, ");
            sub = expr.Substitute("x", "0.5*y-1");
            Console.WriteLine($"f={sub}");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write($"Substitute: y=>sqrt(x), ");
            sub = expr.Substitute("y", "sqrt(x)");
            Console.WriteLine($"f={sub}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        static void TestUnaryFunctions()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Test Unary Functions:");
            Console.ForegroundColor = ConsoleColor.Gray;
            VariableExpr a = "a";
            double x = 2/Math.PI;
            string fstr = $"f({x})", fpstr = $"f'({x})";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"f",-12} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{"f'",-28} ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{fstr,-22} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{fpstr,-28}");
            Console.WriteLine();
            foreach (var op in Enum.GetValues(typeof(UnaryOp)) as UnaryOp[])
            {
                if (op==UnaryOp.Undefined) continue;
                if (op==UnaryOp.Sum) continue;
                var f_expr = Expr.Unary(op, a);
                var dfda = f_expr.Partial(a);
                var f = f_expr[a];
                var fp = dfda[a];
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{f_expr,-12} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{dfda,-28:g4} ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{f(x),-22:g4} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{fp(x),-28:g4}");
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }
        static void TestBinaryFunctions()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Test Binary Functions:");
            Console.ForegroundColor = ConsoleColor.Gray;
            VariableExpr a = "a", b = "b";
            double x = 5, y = 2;
            string fstr = $"f({x},{y})", fpstr = $"f'({x},{y})";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"f",-12} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{"f'",-28} ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{fstr,-22} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{fpstr,-28}");
            Console.WriteLine();
            foreach (var op in Enum.GetValues(typeof(BinaryOp)) as BinaryOp[])
            {
                if (op==BinaryOp.Undefined) continue;
                var f_expr = Expr.Binary(op, a, b);
                var dfda = f_expr.Partial(a);
                var f = f_expr[a, b];
                var fp = dfda[a, b];
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{f_expr,-12} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{dfda,-28:g4} ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{f(x,y),-22:g4} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{fp(x,y),-28:g4}");
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void TestParse1()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Test Parsing Strings");
            Console.ForegroundColor = ConsoleColor.Gray;
            var pi = Expr.Const("pi");
            Console.WriteLine($"π={pi.Value}");

            var s_input = "x+y+z";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Input: {s_input}");
            var s_expr = Expr.Parse(s_input);
            Console.WriteLine($"Expr: {s_expr}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"vars={string.Join(",", s_expr.GetVariables().AsEnumerable())}");
            
            var f_input = "2.5*(1-exp(-π*t))";
            Console.WriteLine($"Input: {f_input}");
            var f_expr = Expr.Parse(f_input);
            var f_fun = f_expr.GetFunction("t");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"x={f_expr}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{"t",-12} {"x",-12}");
            for (int i = 0; i < 10; i++)
            {
                var t = 0.125 * i-0.5;
                var x = f_fun(t);
                Console.WriteLine($"{t,-12:g4} {x,-12:g4}");
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        static void TestParse2()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Test Multivariate Calculus:");
            Console.ForegroundColor = ConsoleColor.Gray;
            VariableExpr x = "x", y="y";
            Console.ForegroundColor = ConsoleColor.Cyan;
            var w_input = "x^2 + 2*x*y + x/y";
            Console.WriteLine($"Input: {w_input}");
            Console.ForegroundColor = ConsoleColor.Gray;
            var w = Expr.Parse(w_input);
            Console.WriteLine($"w={w}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"vars={string.Join(",", w.GetVariables().AsEnumerable())}");
            Console.ForegroundColor = ConsoleColor.Gray;
            var wx = w.Partial(x);
            Console.WriteLine($"wx={wx}");
            var wy = w.Partial(y);
            Console.WriteLine($"wy={wy}");
            var wp = w.TotalDerivative(x, y);
            Console.WriteLine($"wp={wp}");

            var f_input = "(x^2-pi)/(x^2+pi)";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Input: {f_input}");
            Console.ForegroundColor = ConsoleColor.Gray;
            var f = Expr.Parse(f_input);
            Console.WriteLine($"f={f}");
            var fx = f["x"];
            Console.WriteLine($"f(0.5)={fx(0.5)}");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            var df = f.Partial(x);
            Console.WriteLine($"df={df}");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            var fp = f.TotalDerivative();
            Console.WriteLine($"fp={fp}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }
        static void TestParse3()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Test Kinematics:");
            Console.ForegroundColor = ConsoleColor.Gray;
            VariableExpr q = "q", r = "r";
            VariableExpr qp = q.Rate(), rp = r.Rate();
            VariableExpr qpp = qp.Rate(), rpp = rp.Rate();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Variables: {string.Join(", ", q, r)}");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Rates: {string.Join(", ", qp, rp)}");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"Accelerations: {string.Join(", ", qpp, rpp)}");

            Expr pos = Expr.FromArray(r*Expr.Sin(q), -r*Expr.Cos(q));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"pos = {pos}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Expr vel = pos.TotalDerivative();
            Console.WriteLine($"vel = {vel}");
            Console.ForegroundColor = ConsoleColor.Blue;
            Expr J = vel.Jacobian(rp,qp);
            Console.WriteLine($"jacobian = {J}");

            Console.ForegroundColor = ConsoleColor.Green;
            Expr acc = vel.TotalDerivative();
            Console.WriteLine($"acc = {acc}");

            Console.WriteLine();
        }
        static void TestArrayParse1()
        {
            const int colWidth = 12;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Vectorizing Array Parsing:");
            Console.ForegroundColor = ConsoleColor.Gray;
            var input = @"[x^3,6*x^2*(1-x),6*x*(1-x)^2,(1-x)^3]/6";
            Console.WriteLine($"Input: {input}");
            var expr = Expr.Parse(input);
            Console.WriteLine($"y={expr}");
            var f_expr = expr.GetArray("x");
            var d_expr = expr.TotalDerivative();
            Console.WriteLine($"dy={expr}");
            var sum_expr = Expr.Sum(expr);
            var f_sum = sum_expr.GetFunction("x");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Σy={sum_expr}");
            Console.ForegroundColor = ConsoleColor.Gray;
            var d_sum_expr = sum_expr.Partial("x");
            var df_sum = d_sum_expr.GetFunction("x");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"dΣy={d_sum_expr}");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{"x",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int j = 0; j < expr.ResultCount; j++)
            {
                var str = $"y[{j}]";
                Console.Write($"{str,-colWidth} ");
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{"Σy",-colWidth} ");
            Console.Write($"{"dΣy/dx",-colWidth} ");
            Console.WriteLine();
            const int n = 12;
            for (int i = 0; i <= n; i++)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                double x_i = (double)i/n;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write($"{x_i,-colWidth:g4} ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                double[] y_i = f_expr(x_i);
                for (int j = 0; j < y_i.Length; j++)
                {
                    Console.Write($"{y_i[j],-colWidth:g4} ");
                }
                Console.ForegroundColor = ConsoleColor.Cyan;
                double Sy = f_sum(x_i), dSy = df_sum(x_i);
                Console.Write($"{Sy,-colWidth:g4} ");
                Console.Write($"{dSy,-colWidth:g4} ");
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("Dot Product:");

            var ex1 = Expr.FromArray(1, Expr.Sin("x"), Expr.Cos("x"));
            var ex2 = Expr.FromArray("c_0", "c_1", "c_2");
            Console.WriteLine($"ex1={ex1}");
            Console.WriteLine($"ex2={ex2}");
            var y = Expr.Dot(ex1, ex2);

            Console.WriteLine($"Dot(ex1,ex2)={y}");
            var d_y = y.Partial("x");
            Console.WriteLine($"dy/dx={d_y}");
            var yp = y.TotalDerivative();
            //var yp = y.TotalDerivative(
            //    new VariableExpr[] { "x", "c_0", "c_1", "c_2" },
            //    new Expr[] { 1, 0, "cp_1", "cp_2" } );
            Console.WriteLine($"yp={yp}");

            Console.WriteLine();
        }
        static void TestArrayParse2()
        {
            const int colWidth = 12;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Mismatch Array Parsing:");
            Console.ForegroundColor = ConsoleColor.Gray;
            var input = @"[x, 1-x] + [1,2,3]";
            Console.WriteLine($"Input: {input}");
            var expr = Expr.Parse(input);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"y={expr}");
            Console.ForegroundColor = ConsoleColor.Gray;
            var f_expr = expr.GetArray("x");
            var sum_expr = Expr.Sum(expr);
            var f_sum = sum_expr.GetFunction("x");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"Σy={sum_expr}");
            var d_sum_expr = sum_expr.Partial("x");
            var df_sum = d_sum_expr.GetFunction("x");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"dΣy={d_sum_expr}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{"x",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            for (int j = 0; j < expr.ResultCount; j++)
            {
                var str = $"y[{j}]";
                Console.Write($"{str,-colWidth} ");
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{"Σy",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{"dΣy/dx",-colWidth} ");
            Console.WriteLine();
            const int n = 4;
            for (int i = 0; i <= n; i++)
            {
                double x = (double)i/n;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"{x,-colWidth:g4} ");
                double[] y = f_expr(x);
                Console.ForegroundColor = ConsoleColor.Magenta;
                for (int j = 0; j < y.Length; j++)
                {
                    Console.Write($"{y[j],-colWidth:g4} ");
                }
                double Sy = f_sum(x), dSy = df_sum(x);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{Sy,-colWidth:g4} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{dSy,-colWidth:g4} ");
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void TestCubicSpline()
        {
            const int colWidth = 12;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Cubic Spline:");
            Console.ForegroundColor = ConsoleColor.Gray;
            VariableExpr a = "a";

            var xi = new double[] {0, 2};
            var yi = new double[] {1, 7};
            var ypi = new double[] {0, 0};
            var h = xi[1]-xi[0];

            Console.WriteLine("Given Boundary Conditions:");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"{"x",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write($"{"y",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write($"{"yp",-colWidth} ");
            Console.WriteLine();
            for (int i = 0; i < xi.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"{xi[i],-colWidth:g4} ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{yi[i],-colWidth:g4} ");
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write($"{ypi[i],-colWidth:g4} ");
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Calculated Interpolation:");
            Expr ξ = a/h;
            var basis = Expr.FromArray(
                ((1-ξ)^2)*(2*ξ+1),
                (ξ^2)*(3-2*ξ),
                h*ξ*((1-ξ)^2),
                h*(ξ^2)*(ξ-1));
            var coef = new Expr[] { yi[0], yi[1], ypi[0], ypi[1] };

            Expr y_expr = Expr.Dot(basis, coef);
            Console.WriteLine($"y={y_expr}");
            var y_fun = y_expr["a"];
            Expr dy_expr = y_expr.Partial("a");
            Console.WriteLine($"yp={dy_expr}");
            var dy_fun = dy_expr["a"];
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{"x",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{"y",-colWidth} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{"yp",-colWidth} ");
            Console.WriteLine();
            for (int i = 0; i < 12; i++)
            {
                double t = (double)i/11;
                double x = (1-t)*xi[0] + t*xi[1];
                double y = y_fun(x);
                double yp = dy_fun(x);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"{x,-colWidth:g4} ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{y,-colWidth:g4} ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{yp,-colWidth:g4} ");
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

    }
}
