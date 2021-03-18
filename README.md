# JA.Parsing
An expression parser for C# with some ability for simplifications and automatic differentialtion. Inspired by https://github.com/toptensoftware/SimpleExpressionEngine.

## Code Examples

```C#
    VariableExpr x = "x", y="y";

    var f_input = "(x^2-1)/(x^2+1)";
    Console.WriteLine($"input={f_input}");
    var f = Expr.Parse(f_input);
    Console.WriteLine($"f={f}");
    Console.WriteLine($"f(0.5)={f.Eval(("x", 0.5))}");

    var df = f.Partial(x);
    Console.WriteLine($"df={df}");

    var fp = f.TotalDerivative();
    Console.WriteLine($"fp={fp}");

    var w_input = "x^2 + 2*x*y + x/y";
    var w = Expr.Parse(w_input);
    Console.WriteLine($"w={w}");
    var wx = w.Partial(x);
    Console.WriteLine($"wx={wx}");
    var wy = w.Partial(y);
    Console.WriteLine($"wy={wy}");
    var wp = w.Derivative(x, y);
    Console.WriteLine($"wp={wp}");


```
