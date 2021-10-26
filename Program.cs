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
            //FormattingTest();
            //MultiVarTest();
            CalculusExprTest();
            CalculusDerivativeTest();
            CalculusAreaTest();
            CompileArrayDemo();
            CompileMatrixDemo();
        }

        private static void CalculusExprTest()
        {
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
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
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
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
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
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
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
            SymbolExpr x = "x", y = "y";
            double a = 3, b = 0.25;

            Console.WriteLine($"a={a}, b={b}, {x}, {y}");

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
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
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
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
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
        static void CalculusDerivativeTest()
        {
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
            SymbolExpr x = "x";
            foreach (var op in KnownUnaryDictionary.Defined)
            {
                var f = Expr.Unary(op, x);
                var fp = f.PartialDerivative(x);
                Console.WriteLine($"d/dx({f}) = {fp}");
            }
            Console.WriteLine();
        }
        static void CalculusAreaTest()
        {
            Console.WriteLine($"*** TEST [{++testIndex}] : {GetMethodName()} ***");
            // Parametrize a 2D region and calculate it's Area

            // Consider a triange between the origin, and two points
            //  A = [5,0]
            //  B = [2,3]
            Expr t = "t";
            Expr u = "u";
            Vector A = new Vector(5,0);
            Vector B = new Vector(2,3);
            Expr pos = t*((1-u)*A + u*B );
            Console.WriteLine($"pos(t,u) = {pos}");
            Expr dA = Expr.Cross( pos.PartialDerivative(t).ToArray(), pos.PartialDerivative(u).ToArray() );
            Console.WriteLine($"dA=({dA}) dt du");
            Expr J = dA.Jacobian();
            Console.WriteLine($"J={J}");
            Console.WriteLine();
        }

        static string GetMethodName([CallerMemberName] string name = null) 
        {
            return name;
        }
    }
}
