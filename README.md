# JA.Parsing

An expression parser for `C#` with some ability for simplifications and automatic differentialtion. Inspired by [https://github.com/toptensoftware/SimpleExpressionEngine](https://github.com/toptensoftware/SimpleExpressionEngine).

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

## Highlights

The main type is `Expr` that holds expression trees. An auxilary type is `VariableExpr` which holds variables. A string input is parsed using `Expr.Parse()` and a variable is declared using implicit conversion `Expr x = "x";` or by calling `Expr x = Expr.Variable("x");`.

Several build-in constants are defined similarly to variables. They are accessible with the following function call `Expr.Const("pi")` or by a property `Expr.Pi`.

```c#
  inf = ∞
  nan = NaN
   pi = 3.14159265358979
divpi = 0.318309886183791
    e = 2.71828182845905
    Φ = 1.61803398874989
  deg = 0.0174532925199433
  rad = 57.2957795130823
  rpm = 0.10471975511966
```

Each expression has a `.Substiute(target,expression)`  function that can perform expression substitutions. Numerical evaluation is done with `.Eval( <name-value pairs> )` where multiple arguments of `(string symbol, double value)` types is required in order to fully evaluate all variables in the expression tree. A shortcut to `f.Eval()` is the default indexer `f[]`.


## Calculus

Besides standard trig functions such as `Expr.Sin(x)` each `Expr` can evaluate both the partial derivative with respect to a variable with `f.Partial(x)` or the total derivative with `f.TotalDerivative()`.

The ****total derivative**** needs the derivative of each variable (rate) which is automatically defined by appending the character `p`  to the name of the variable. Then it performs the chain rule, such as

```C#
fp = f.Partial(x)*xp + f.Partial(y)*yp + ...
```

You can define the names of variable rates by either a list of `varaible-rate` tuples, or 2nd argument with an array of rates. The rates can be just variables names, or can be any expression. For example, if the rate of time is `1` then you can write

```
var q = new VariableExpr[] { "t", "x", "y" }
var qp = new Expr[] { 1, "xp", "yp" }
fp = f.TotalDerivative(q, qp);
```
