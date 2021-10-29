using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Linq;
using JA.Expressions;

namespace JA
{

    static class Program
    {
        static int testIndex = 0;
        static void Main(string[] args)
        {

            ParseExpressionDemo();
            CompileExpressionDemo();
            SimplifyExpressionDemo();
            AssignExpressionDemo();
            SystemOfEquationsDemo();
            //FormattingTest();
            //MultiVarTest();
            CalculusExprDemo();
            CalculusDerivativeDemo();
            CalculusSolveDemo();
            CompileArrayDemo();
            CompileMatrixDemo();
        }


        private static void CalculusExprDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            SymbolExpr x = "x", y="y";

            var f_input = "((x-1)*(x+1))/((x)^2+1)";
            Console.WriteLine($"input: {f_input}");
            var f = Expr.Parse(f_input).GetFunction("f");
            Console.WriteLine(f);
            var fx = f.CompileArg1();
            Console.WriteLine($"f(0.5)={fx(0.5)}");

            Console.WriteLine("Partial Derivative");
            var df = f.PartialDerivative(x);
            Console.WriteLine($"df/dx: {df}");

            Console.WriteLine("Total Derivative");
            var fp = f.TotalDerivative();
            Console.WriteLine(fp);

            Console.WriteLine();
            var w_input = "x^2 + 2*x*y + x/y";
            Console.WriteLine($"input: {w_input}");
            var w = Expr.Parse(w_input).GetFunction("w", "x", "y");
            Console.WriteLine(w);

            Console.WriteLine("Patial Derivatives");
            var wx = w.PartialDerivative(x);
            Console.WriteLine($"dw/dx: {wx}");
            var wy = w.PartialDerivative(y);
            Console.WriteLine($"dw/dy: {wy}");

            Console.WriteLine("Total Derivative");
            Console.WriteLine($"Set xp=v, yp=3");
            var wp = w.TotalDerivative("wp", ("x", "v"), ("y", 3.0));
            Console.WriteLine(wp);
            Console.WriteLine();
        }

        static void ParseExpressionDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            Console.WriteLine("Evaluate Expression");
            var text = "1/t^2*(1-exp(-pi*t))/(1-t^2)";
            var expr = Expr.Parse(text);
            Console.WriteLine(expr);
            Console.WriteLine($"Symbols: {string.Join(",", expr.GetSymbols())}");
            Console.WriteLine($"Rank:{expr.Rank}");
            Console.WriteLine($"{"t",-12} {"expr",-12}");
            for (int i = 1; i <= 10; i++)
            {
                var t = 0.10 * i;
                var x = expr.Eval(("t", t));
                Console.WriteLine($"{t,-12:g4} {x,-12:g4}");
            }
            Console.WriteLine();
        }
        static void CompileExpressionDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            Console.WriteLine("Compile Expression");
            var text = "1/t^2*(1-exp(-pi*t))/(1-t^2)";
            var expr = Expr.Parse(text);
            var fun = new Function("f", expr, "t");
            Console.WriteLine(fun);
            Console.WriteLine($"Rank:{fun.Rank}");
            var fq = fun.Compile<FArg1>();
            Console.WriteLine($"{"t",-12} {"f(t)",-12}");
            for (int i = 1; i <= 10; i++)
            {
                var t = 0.10 * i;
                var x = fq(t);
                Console.WriteLine($"{t,-12:g4} {x,-12:g4}");
            }
            Console.WriteLine();
        }
        static void SimplifyExpressionDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            SymbolExpr x = "x", y = "y";
            //double a = 3, b = 0.25;
            Expr a = "a=3", b = "b=0.25";

            Console.WriteLine($"{a}={(double)a}, {b}={(double)b}, {x}, {y}");

            Console.WriteLine();
            int index = 0;
            Console.WriteLine($"{index,3}.     definition = result");
            Console.WriteLine($"{++index,3}. ({a}+x)+({b}+y) = {(a+x)+(b+y)}");
            Console.WriteLine($"{++index,3}. ({a}+x)-({b}+y) = {(a+x)-(b+y)}");
            Console.WriteLine($"{++index,3}. ({a}-x)+({b}+y) = {(a-x)+(b+y)}");
            Console.WriteLine($"{++index,3}. ({a}+x)-({b}-y) = {(a+x)-(b-y)}");
            Console.WriteLine($"{++index,3}. ({a}-x)+({b}-y) = {(a-x)+(b-y)}");
            Console.WriteLine($"{++index,3}. ({a}-x)-({b}-y) = {(a-x)-(b-y)}");
            Console.WriteLine($"{++index,3}. ({a}*x)+({b}*y) = {(a*x)+(b*y)}");
            Console.WriteLine($"{++index,3}. ({a}*x)-({b}*y) = {(a*x)-(b*y)}");
            Console.WriteLine($"{++index,3}. ({a}/x)+({b}*y) = {(a/x)+(b*y)}");
            Console.WriteLine($"{++index,3}. ({a}*x)-({b}/y) = {(a*x)-(b/y)}");
            Console.WriteLine($"{++index,3}. ({a}/x)+({b}/y) = {(a/x)+(b/y)}");
            Console.WriteLine($"{++index,3}. ({a}/x)-({b}/y) = {(a/x)-(b/y)}");

            Console.WriteLine($"{++index,3}. ({a}+x)*({b}+y) = {(a+x)*(b+y)}");
            Console.WriteLine($"{++index,3}. ({a}+x)/({b}+y) = {(a+x)/(b+y)}");
            Console.WriteLine($"{++index,3}. ({a}-x)*({b}+y) = {(a-x)*(b+y)}");
            Console.WriteLine($"{++index,3}. ({a}+x)/({b}-y) = {(a+x)/(b-y)}");
            Console.WriteLine($"{++index,3}. ({a}-x)*({b}-y) = {(a-x)*(b-y)}");
            Console.WriteLine($"{++index,3}. ({a}-x)/({b}-y) = {(a-x)/(b-y)}");
            Console.WriteLine($"{++index,3}. ({a}*x)*({b}*y) = {(a*x)*(b*y)}");
            Console.WriteLine($"{++index,3}. ({a}*x)/({b}*y) = {(a*x)/(b*y)}");
            Console.WriteLine($"{++index,3}. ({a}/x)*({b}*y) = {(a/x)*(b*y)}");
            Console.WriteLine($"{++index,3}. ({a}*x)/({b}/y) = {(a*x)/(b/y)}");
            Console.WriteLine($"{++index,3}. ({a}/x)*({b}/y) = {(a/x)*(b/y)}");
            Console.WriteLine($"{++index,3}. ({a}/x)/({b}/y) = {(a/x)/(b/y)}");
            Console.WriteLine();
        }
        static void CompileArrayDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            Console.WriteLine("Array Expression");
            var text = "abs([(1-t)^3,3*(1-t)^2*t,3*t^2*(1-t),t^3])";
            var expr = Expr.Parse(text);
            var y = expr.Eval(("t", 0.5));
            var fun = new Function("f", expr, "t");
            Console.WriteLine(fun);
            var f = fun.Compile<QArg1>();
            Console.WriteLine($"{"t",-12} {"f(t)",-12}");
            for (int i = 0; i <= 10; i++)
            {
                var t = 0.10 * i;
                var x = f(t);
                Console.WriteLine($"{t,-12:g4} {x,-18:g4}");
            }
            Console.WriteLine();
        }

        static void CompileMatrixDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            Console.WriteLine("Matrix Expression");
            var tt = Expr.Variable("t");
            var expr = Expr.Matrix( new Expr[][] {
                new Expr[] { 1/(1+tt^2), 1-(tt^2)/(1+tt^2) },
                new Expr[] { tt/(1+tt^2), -1/(1+tt^2) }});

            var y = expr.Eval((tt, 0.5));

            var fun = new Function("f", expr, "t");
            Console.WriteLine(fun);
            var f = fun.Compile<QArg1>();
            Console.WriteLine($"{"t",-12} {"f(t)",-12}");
            for (int i = 0; i <= 10; i++)
            {
                var t = 0.10 * i;
                var x = f(t);
                Console.WriteLine($"{t,-12:g4} {x,-26:g4}");
            }

            var fp = fun.PartialDerivative(tt);
            Console.WriteLine(fp);
            Console.WriteLine();
        }
        static void CalculusDerivativeDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            SymbolExpr x = "x";
            foreach (var op in KnownUnaryDictionary.Defined)
            {
                var f = Expr.Unary(op, x);
                var fp = f.PartialDerivative(x);
                Console.WriteLine($"d/dx({f}) = {fp}");
            }
            Console.WriteLine();
        }
        static void CalculusSolveDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");

            Console.WriteLine("Define a function and solve using Newton-Raphon.");

            Expr a = 7, b= 3;
            Expr x = "x";

            Function f = ( a*Expr.Sin(x)-b*x ).GetFunction("f");
            Console.WriteLine(f);

            Console.WriteLine("Find solution, such that f(x)=0");

            Scalar init = 3.0;
            Console.WriteLine($"Initial guess, x={init}");
            Scalar sol = (Scalar)f.NewtonRaphson(init, 0.0);

            var fx = f.CompileArg1();
            Console.WriteLine($"x={sol}, f(x)={fx(sol)}");

            Console.WriteLine();
        }

        static void AssignExpressionDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");

            Expr.ClearVariables();

            var input = "a+b = (2*a+2*b)/2";
            var ex_1 = Expr.Parse(input);
            Console.WriteLine(ex_1);
            var e_1 = ex_1.Eval(("a",1), ("b",3) );
            Console.WriteLine($"Eval = {e_1}");

            Console.WriteLine("Use = for assignment of constants.");

            SymbolExpr a = "a", b= "b", c="c";
            Expr lhs = Expr.Array(a,b,c);
            Expr rhs = "[1,2,3]";
            Console.WriteLine($"{lhs}={rhs}");
            Expr ex_2 = Expr.Assign(lhs, rhs);
            Console.WriteLine(ex_2);

            input = "(a+b)*x-c";
            var f = Expr.Parse(input).GetFunction("f", "x");
            Console.WriteLine(f);
            var fx = f.CompileArg1();
            foreach (var x in new[] { -1.0, 0.0, 1.0 })
            {
                Console.WriteLine($"f({x})={fx(x)}");
            }

            Console.WriteLine();
        }
        static void SystemOfEquationsDemo()
        {
            Console.WriteLine($"*** DEMO [{++testIndex}] : {GetMethodName()} ***");
            Expr.ClearVariables();

            Console.WriteLine("Define a 3×3 system of equations.");
            var system = Expr.Parse("[2*x-y+3*z=15, x + 3*z/2 = 3, x+3*y = 1]");
            var vars = new[] { "x", "y", "z" };

            Console.WriteLine(system);
            Console.WriteLine();
            if (system.ExtractLinearSystem(vars, out Matrix A, out Vector b))
            {
                Console.WriteLine($"Unknowns: {string.Join(",", vars)}");
                Console.WriteLine();
                ShowMatrix("Coefficient Matrix A=", A);

                Console.WriteLine();
                ShowVector("Constant Vector b=", b);

                var x = A.Solve(b);
                ShowVector("Solution Vector x=", x);

                var r = b-A*x;
                ShowVector("Residual Vector r=", r);
            }

            Console.WriteLine();

        }

        static void ShowVector(string title, Vector x, string formatting = "g4", int columnWidth = 6)
        {
            Console.WriteLine(title);
            for (int i = 0; i < x.Size; i++)
            {
                Console.WriteLine($"| {x.Elements[i].ToString(formatting).PadLeft(columnWidth)} |");
            }
        }
        static void ShowMatrix(string title, Matrix A, string formatting = "g4", int columnWidth = 6)
        {
            Console.WriteLine(title);
            for (int i = 0; i < A.Rows; i++)
            {
                Console.WriteLine($"| {string.Join(" ", A.Elements[i].Select(x=>x.ToString(formatting).PadLeft(columnWidth)))} |");
            }
        }

        static string GetMethodName([CallerMemberName] string name = null)
        {
            return name;
        }
    }
}
