using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime;
using System.Reflection;
using System.Reflection.Emit;

namespace JA.Parsing
{
    using static System.Math;

    public static class MathExtra
    {
        public static double Asinh(double x) => Math.Log(x + Math.Sqrt(x* x+1));
        public static double Acosh(double x) => Math.Log(x + Math.Sqrt(x* x-1));
        public static double Atanh(double x) => -Math.Log((1-x)/(1+x))/2;
        public static double Sign(double x, double y) => Math.Abs(x)*Math.Sign(y);
    }

    public abstract class Expr :
        IFormattable,
        IEquatable<Expr>
    {
        public static IReadOnlyCollection<(string sym, double val)> Constants
            => new ReadOnlyCollection<(string sym, double val)>(NamedConstantExpr.Defined.Select((item) => (item.Key, item.Value.Value)).ToList());


        #region Factory
        // Static helper to parse a string
        public static Expr Parse(string str)
        {
            return Parse(new Tokenizer(str));
        }

        // Static helper to parse from a tokenizer
        public static Expr Parse(Tokenizer tokenizer)
        {
            var parser = new Parser(tokenizer);
            return parser.ParseExpression();
        }

        public static Expr Unary(UnaryOp op, Expr argument)
        {
            if (ArrayExpr.IsVectorizable(argument, out int count, out var argArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Unary(op, argArray[i]);
                }
                return FromArray(vector);
            }

            switch (op)
            {
                case UnaryOp.Identity: return argument;
                case UnaryOp.Negate: return Negate(argument);
                case UnaryOp.Inverse: return Inv(argument);
                case UnaryOp.Rnd: return Random(argument);
                case UnaryOp.Pi: return Pi(argument);
                case UnaryOp.Abs: return Abs(argument);
                case UnaryOp.Sign: return Sign(argument);
                case UnaryOp.Exp: return Exp(argument);
                case UnaryOp.Log: return Log(argument);
                case UnaryOp.Log2: return Log2(argument);
                case UnaryOp.Log10: return Log10(argument);
                case UnaryOp.Sqr: return Sqr(argument);
                case UnaryOp.Cub: return Cub(argument);
                case UnaryOp.Sqrt: return Sqrt(argument);
                case UnaryOp.Cbrt: return Cbrt(argument);
                case UnaryOp.Floor: return Floor(argument);
                case UnaryOp.Ceiling: return Ceiling(argument);
                case UnaryOp.Round: return Round(argument);
                case UnaryOp.Sin: return Sin(argument);
                case UnaryOp.Cos: return Cos(argument);
                case UnaryOp.Tan: return Tan(argument);
                case UnaryOp.Sinh: return Sinh(argument);
                case UnaryOp.Cosh: return Cosh(argument);
                case UnaryOp.Tanh: return Tanh(argument);
                case UnaryOp.Asin: return Asin(argument);
                case UnaryOp.Acos: return Acos(argument);
                case UnaryOp.Atan: return Atan(argument);
                case UnaryOp.Asinh: return Asinh(argument);
                case UnaryOp.Acosh: return Acosh(argument);
                case UnaryOp.Atanh: return Atanh(argument);
            }
            return new UnaryExpr(op, argument);
        }

        public static Expr Binary(BinaryOp op, Expr left, Expr right)
        {
            if (ArrayExpr.IsVectorizable(left, right, out int count, out var leftArray, out var rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Binary(op, leftArray[i], rightArray[i]);
                }
                return FromArray(vector);
            }

            switch (op)
            {
                case BinaryOp.Add: return Add(left, right);
                case BinaryOp.Subtract: return Subtract(left, right);
                case BinaryOp.Multiply: return Multiply(left, right);
                case BinaryOp.Divide: return Divide(left, right);
                case BinaryOp.Max: return Max(left, right);
                case BinaryOp.Min: return Min(left, right);
                case BinaryOp.Pow: return Power(left, right);
                case BinaryOp.Sign: return Sign(left, right);
                case BinaryOp.Log: return Log(left, right);
            }
            return new BinaryExpr(op, left, right);
        }
        public static implicit operator Expr(double x) => Number(x);
        public static implicit operator Expr(string expr) => Parse(expr);
        public static implicit operator Expr(string[] expressions)
            => FromArray(expressions);
        public static implicit operator Expr(Expr[] expressions)
            => FromArray(expressions);

        #endregion

        #region Convenience Helpers

        protected static readonly Random rng = new Random();


        readonly static Type mathType = typeof(Math);
        protected static MethodInfo GetOpMethod(UnaryOp op)
        {
            return mathType.GetMethod(op.ToString(), new[] { typeof(double) })
                ?? UnaryExpr.functions[op].Method;
        }
        protected static MethodInfo GetOpMethod(BinaryOp op)
        {
            return mathType.GetMethod(op.ToString(), new[] { typeof(double), typeof(double) })
                ?? BinaryExpr.functions[op].GetMethodInfo();
        }
        public bool IsArray(out Expr[] elements)
        {
            if (this is ArrayExpr ae)
            {
                elements = ae.Elements;
                return true;
            }
            elements = Array.Empty<Expr>();
            return false;
        }
        public bool IsConst(out double value)
        {
            if (this is ConstExpr ce)
            {
                value = ce.Value;
                return true;
            }
            value = double.NaN;
            return false;
        }
        public bool IsVariable(out string symbol)
        {
            if (this is VariableExpr v)
            {
                symbol = v.Symbol;
                return true;
            }
            symbol = null;
            return false;
        }
        public bool IsUnary(UnaryOp op, out Expr argument)
        {
            if (this is UnaryExpr ue && ue.Op == op)
            {
                argument = ue.Argument;
                return true;
            }
            argument = null;
            return false;
        }
        public bool IsUnary(out UnaryOp op, out Expr argument)
        {
            if (this is UnaryExpr ue)
            {
                argument = ue.Argument;
                op = ue.Op;
                return true;
            }
            op = UnaryOp.Undefined;
            argument = null;
            return false;
        }
        public bool IsBinary(BinaryOp op, out Expr left, out Expr right)
        {
            if (this is BinaryExpr be && be.Op == op)
            {
                left = be.Left;
                right = be.Right;
                return true;
            }
            left = null;
            right  = null;
            return false;
        }
        public bool IsBinary(out BinaryOp op, out Expr left, out Expr right)
        {
            if (this is BinaryExpr be)
            {
                op = be.Op;
                left = be.Left;
                right = be.Right;
                return true;
            }
            op = BinaryOp.Undefined;
            left = null;
            right  = null;
            return false;
        }

        /// <summary>
        /// Determines whether the specified expression has a factor. For example <c>2*x => (Factor=2, Argument=x)</c>
        /// </summary>
        /// <param name="factor">The factor.</param>
        /// <param name="argument">The argument.</param>
        public bool IsFactor(out double factor, out Expr argument)
        {
            if (this is UnaryExpr ue)
            {
                argument = ue.Argument;
                switch (ue.Op)
                {
                    case UnaryOp.Identity:
                        factor = 1;
                        break;
                    case UnaryOp.Negate:
                        factor =-1;
                        break;
                    case UnaryOp.Pi:
                        factor = Math.PI;
                        break;
                    default:
                        factor = 0;
                        break;
                }
                return factor!=0;
            }
            if (this is BinaryExpr be)
            {
                if (be.Op == BinaryOp.Multiply)
                {
                    if (be.Left.IsConst(out var lx))
                    {
                        factor = lx;
                        argument = be.Right;
                        return true;
                    }
                    if (be.Right.IsConst(out var rx))
                    {
                        factor = rx;
                        argument = be.Left;
                        return true;
                    }
                }
                if (be.Op == BinaryOp.Divide)
                {
                    if (be.Left.IsConst(out var lx))
                    {
                        factor = lx;
                        argument = 1/be.Right;
                        return true;
                    }
                    if (be.Right.IsConst(out var rx))
                    {
                        factor = 1/rx;
                        argument = be.Left;
                        return true;
                    }
                }
                if (be.Op == BinaryOp.Sign && be.Right.IsConst(out var f))
                {
                    // (x,y) => |x|*Sign(y) = factor*x
                    factor = Math.Sign(f);
                    argument = Abs(be.Left);
                    return true;
                }
            }
            factor = 0;
            argument = null;
            return false;
        }
        public bool IsRaised(out Expr argument, out double exponent)
        {
            if (this is UnaryExpr ue)
            {
                argument = ue.Argument;
                switch (ue.Op)
                {
                    case UnaryOp.Identity:
                        exponent = 1;
                        break;
                    case UnaryOp.Sqr:
                        exponent = 2;
                        break;
                    case UnaryOp.Cub:
                        exponent = 3;
                        break;
                    case UnaryOp.Sqrt:
                        exponent = 1/2.0;
                        break;
                    case UnaryOp.Cbrt:
                        exponent = 1/3.0;
                        break;
                    default:
                        exponent = 0;
                        break;
                }
                return exponent!=0;
            }
            if (this is BinaryExpr be && be.Op == BinaryOp.Pow)
            {
                if (be.Right.IsConst(out var exp))
                {
                    exponent = exp;
                    argument = be.Left;
                    return true;
                }
            }
            exponent = 0;
            argument = null;
            return false;
        }

        public static bool HaveCommonFactor(Expr left, Expr right, out Expr argument, out double leftFactor, out double rightFactor)
        {
            // (a*x) $ (b*x)
            if (left.IsFactor(out leftFactor, out var leftArg)
                && right.IsFactor(out rightFactor, out var rightArg))
            {
                if (leftArg.Equals(rightArg))
                {
                    argument = leftArg;
                    return true;
                }
            }
            argument = null;
            leftFactor = double.NaN;
            rightFactor = double.NaN;
            return false;
        }

        public static bool HaveCommonExponent(Expr left, Expr right, out Expr leftArg, out Expr rightArg, out double exponent)
        {
            // (x^a) $ (y^a)
            if (left.IsRaised(out leftArg, out double leftExp)
                && right.IsRaised(out rightArg, out double rightExp))
            {
                if (leftExp==rightExp)
                {
                    exponent = leftExp;
                    return true;
                }
            }
            leftArg = null;
            rightArg = null;
            exponent = double.NaN;
            return false;
        }
        public static bool HaveCommonBase(Expr left, Expr right, out Expr @base, out double leftExp, out double rightExp)
        {
            // (x^a) $ (y^a)
            if (left.IsRaised(out var leftArg, out leftExp)
                && right.IsRaised(out var rightArg, out rightExp))
            {
                if (leftArg.Equals(rightArg))
                {
                    @base = leftArg;
                    return true;
                }
            }
            @base = null;
            leftExp = double.NaN;
            rightExp = double.NaN;
            return false;
        }

        public static bool AreSimilar(Expr left, Expr right)
        {
            return AreConstants(left, right, out _, out _)
                || AreVariables(left, right, out _, out _)
                || AreUnary(left, right, out _, out _, out _)
                || AreBinary(left, right, out _, out _, out _, out _, out _)
                || AreArrays(left, right, out _, out _);
        }

        public static bool AreConstants(Expr left, Expr right, out double leftValue, out double rightValue)
        {
            if (left is ConstExpr ce && right is ConstExpr co)
            {
                leftValue = ce.Value;
                rightValue = co.Value;
                return true;
            }
            leftValue = double.NaN;
            rightValue = double.NaN;
            return false;
        }
        public static bool AreVariables(Expr left, Expr right, out string leftSymbol, out string rightSymbol)
        {
            if (left is VariableExpr ce && right is VariableExpr co)
            {
                leftSymbol = ce.Symbol;
                rightSymbol = co.Symbol;
                return true;
            }
            leftSymbol = null;
            rightSymbol = null;
            return false;
        }
        public static bool AreUnary(Expr left, Expr right, UnaryOp op, out Expr leftArgument, out Expr rightArgument) 
        {
            if (AreUnary(left, right, out UnaryOp uop, out leftArgument, out rightArgument))
            {
                return uop == op;
            }
            return false;
        }
        public static bool AreUnary(Expr left, Expr right, out UnaryOp op, out Expr leftArgument, out Expr rightArgument)
        {
            if (left is UnaryExpr ue && right is UnaryExpr uo)
            {
                op = ue.Op;
                leftArgument = ue.Argument;
                rightArgument = uo.Argument;
                return ue.Op == uo.Op;
            }
            leftArgument = null;
            rightArgument = null;
            op = UnaryOp.Undefined;
            return false;
        }
        public static bool AreBinary(Expr left, Expr right, BinaryOp op, out Expr leftLeft, out Expr leftRight, out Expr rightLeft, out Expr rightRight) 
        {
            if (AreBinary(left, right, out BinaryOp bop, out leftLeft, out leftRight, out rightLeft, out rightRight))
            {
                return bop == op;
            }
            return false;
        }
        public static bool AreBinary(Expr left, Expr right, out BinaryOp op, out Expr leftLeft, out Expr leftRight, out Expr rightLeft, out Expr rightRight)
        {
            if (left is BinaryExpr be && right is BinaryExpr bo)
            {
                op = be.Op;
                leftLeft = be.Left;
                leftRight = be.Right;
                rightLeft = bo.Left;
                rightRight = bo.Right;
                return be.Op == bo.Op;
            }
            op = BinaryOp.Undefined;
            leftLeft   = null;
            leftRight  = null;
            rightLeft  = null;
            rightRight = null;
            return false;
        }
        public static bool AreArrays(Expr left, Expr right, out Expr[] leftArray, out Expr[] rightArray)
        {
            if (left.IsArray(out leftArray) && right.IsArray(out rightArray))
            {
                return true;
            }
            leftArray = null;
            rightArray = null;
            return false;
        }

        #endregion

        #region Properties

        public abstract int ResultCount { get; }

        public Func<double, double> this[string var1]
        {
            get => GetFunction(var1);
        }
        public Func<double, double, double> this[string var1, string var2]
        {
            get => GetFunction(var1, var2);
        }
        public Func<double, double, double, double> this[string var1, string var2, string var3]
        {
            get => GetFunction(var1, var2, var3);
        }
        public Func<double, double, double, double, double> this[string var1, string var2, string var3, string var4]
        {
            get => GetFunction(var1, var2, var3, var4);
        } 
        #endregion

        #region Methods
        public Func<double> 
            GetFunction()
            => Compile<Func<double>>(Array.Empty<VariableExpr>());

        public Func<double, double> 
            GetFunction(VariableExpr var1)
            => Compile<Func<double, double>>(var1);

        public Func<double, double, double> 
            GetFunction(VariableExpr var1, VariableExpr var2)
            => Compile<Func<double, double, double>>(var1, var2);
        public Func<double, double, double, double> 

            GetFunction(VariableExpr var1, VariableExpr var2, VariableExpr var3)
            => Compile<Func<double, double, double, double>>(var1, var2, var3);
        public Func<double, double, double, double, double> 

            GetFunction(VariableExpr var1, VariableExpr var2, VariableExpr var3, VariableExpr var4)
            => Compile<Func<double, double, double, double, double>>(var1, var2, var3, var4);

        public Func<double[]> GetArray()
            => Compile<Func<double[]>>(Array.Empty<VariableExpr>());

        public Func<double, double[]>
            GetArray(VariableExpr var1)
            => Compile<Func<double, double[]>>(var1);

        public Func<double, double, double[]>
            GetArray(VariableExpr var1, VariableExpr var2)
            => Compile<Func<double, double, double[]>>(var1, var2);
        
        public Func<double, double, double, double[]>
            GetArray(VariableExpr var1, VariableExpr var2, VariableExpr var3)
            => Compile<Func<double, double, double, double[]>>(var1, var2, var3);

        public Func<double, double, double, double, double[]>
            GetArray(VariableExpr var1, VariableExpr var2, VariableExpr var3, VariableExpr var4)
            => Compile<Func<double, double, double, double, double[]>>(var1, var2, var3, var4);

        public T Compile<T>(params string[] parameters)
        {
            return Compile<T>(parameters.Select((arg) => Variable(arg)).ToArray());
        }
        public T Compile<T>(params VariableExpr[] parameters)
        {
            var asy = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("ExprAsy"),
                AssemblyBuilderAccess.RunAndCollect);

            var module = asy.DefineDynamicModule("ExprAsyMod");
            var type = module.DefineType("Code");
            var ret = typeof(double);
            if (ResultCount>1)
            {
                ret = ret.MakeArrayType();
            }
            var method = type.DefineMethod("Generation",
                MethodAttributes.Public | MethodAttributes.Static,
                ret,
                Enumerable.Repeat(typeof(double), parameters.Length).ToArray());

            var envirnoment = new Dictionary<VariableExpr, int>();
            int index = 0;
            foreach (var item in parameters)
            {
                envirnoment[item] = index++;
            }
            var generator = method.GetILGenerator();
            Compile(generator, envirnoment);
            generator.Emit(OpCodes.Ret);            
            var res = type.CreateType();

            return (T)(object)Delegate.CreateDelegate(
                typeof(T),
                res.GetMethod("Generation"));
        }

        protected internal abstract void Compile(ILGenerator generator, Dictionary<VariableExpr, int> envirnoment);

        public VariableExpr[] GetVariables()
        {
            var list = new List<VariableExpr>();
            AddVariables(list);
            return list.ToArray();
        }
        protected internal abstract void AddVariables(List<VariableExpr> variables);
        /// <summary>
        /// Substitutes an expression each time the target (or variable) is found in the whole expression tree.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="expression">The expression.</param>
        /// <example>
        ///   <br />
        ///   <code title="Example for `Substitute`">Expr x = "x";
        /// Expr y = (1+x).Substitute(x, x^2);
        /// </code>
        /// </example>
        public abstract Expr Substitute(VariableExpr target, Expr expression);
        public Expr Substitute(string symbol, Expr expression) 
            => Substitute(Variable(symbol), expression);
        /// <summary>
        /// Get the partial derivative of the expression with respect to a variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <example>
        ///   <code>Expr x = "x";
        /// Expr y = (x^2).Partial(x);
        /// </code>
        /// </example>
        public abstract Expr Partial(VariableExpr variable);
        public Expr Partial(string symbol) => Partial(Variable(symbol));

        /// <summary>Calculate the total derivative using the chain rule.</summary>
        /// <param name="variables">The variables to differentiate for.</param>
        /// <param name="rates">The derivative of the variables</param>
        public Expr TotalDerivative(VariableExpr[] variables, Expr[] rates)
        {
            Expr sum = 0;
            for (int i = 0; i < variables.Length; i++)
            {
                var x = variables[i];
                var xp = rates[i];
                sum += Partial(x) * xp;
            }
            return sum;
        }
        public Expr TotalDerivative(params VariableExpr[] variables)
        {
            var rates = variables.Select((x) => x.Rate()).ToArray();
            return TotalDerivative(variables, rates);
        }
        public Expr TotalDerivative(params string[] variables)
        {
            return TotalDerivative(variables.Select((sym) => Variable(sym)).ToArray());
        }
        public Expr TotalDerivative()
        {
            var variables = GetVariables();
            var rates = variables.Select((x) => x.Rate()).ToArray();
            return TotalDerivative(variables, rates);
        }
        public Expr TotalDerivative(params (VariableExpr x, Expr xp)[] variables)
        {
            Expr sum = 0;
            for (int i = 0; i < variables.Length; i++)
            {
                var x = variables[i].x;
                var xp = variables[i].xp;
                sum += Partial(x) * xp;
            }
            return sum;
        }
        public Expr[] Jacobian(params string[] variables)
        {
            var jacobian = new Expr[variables.Length];
            for (int i = 0; i < jacobian.Length; i++)
            {
                jacobian[i] = Partial(variables[i]);
            }
            return jacobian;
        }
        #endregion

        #region Variables & Constants

        /// <summary>Define a constant value.</summary>
        /// <param name="value">The value.</param>
        public static ConstExpr Number(double value) => new ConstExpr(value);
        /// <summary>Defined a named constant, for example 'pi=3.141.."</summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static Expr Const(string name, double value)
        {
            return new NamedConstantExpr(name, value);
        }
        /// <summary>
        /// Retrieve a named constant from its name
        /// </summary>
        /// <param name="name">The name</param>
        /// <example>
        /// <code>
        /// Expr pi = Const("pi");
        /// </code>
        /// </example>
        public static NamedConstantExpr Const(string name) => NamedConstantExpr.Defined[name];
        public static Expr VariableOrConst(string name)
        {
            if (NamedConstantExpr.Defined.ContainsKey(name))
            {
                return NamedConstantExpr.Defined[name];
            }
            return Variable(name);
        }

        /// <summary>Defines a variable from a name. Cannot be one of the named constants.</summary>
        /// <param name="symbol">The variable name (symbol).</param>
        /// <exception cref="ArgumentException">symbol is used as a named constant.</exception>
        public static VariableExpr Variable(string symbol)
        {
            if (NamedConstantExpr.Defined.ContainsKey(symbol))
            {
                // try to define a variable that is used 
                // as a constant, like 'pi'.
                throw new ArgumentException($"{symbol} is reserved as a named constant.", nameof(symbol));
            }
            if (VariableExpr.Defined.ContainsKey(symbol))
            {
                return VariableExpr.Defined[symbol];
            }
            var tok = new Tokenizer(symbol);
            if (tok.Current.Token == Token.Identifier)
            {
                return new VariableExpr(tok.Current.Identifier);
            }            
            throw new ArgumentException($"Expected a symbol and got {symbol}.", nameof(symbol));
        }
        public static Expr FromArray(IEnumerable<Expr> expressions)
            => new ArrayExpr(expressions.ToArray());
        public static Expr FromArray(params Expr[] expressions)
            => new ArrayExpr(expressions);

        public static readonly ConstExpr Zero = Number(0);
        public static readonly ConstExpr One = Number(1);
        public static readonly NamedConstantExpr Inf   = new NamedConstantExpr("inf", double.PositiveInfinity);
        public static readonly NamedConstantExpr Nan   = new NamedConstantExpr("nan", double.NaN);
        public static readonly NamedConstantExpr PI    = new NamedConstantExpr("pi", Math.PI);
        public static readonly NamedConstantExpr π     = new NamedConstantExpr("π", Math.PI);
        public static readonly NamedConstantExpr DivPI = new NamedConstantExpr("divpi", 1/Math.PI);
        public static readonly NamedConstantExpr E     = new NamedConstantExpr("e", Math.E);
        public static readonly NamedConstantExpr Φ     = new NamedConstantExpr("Φ", (1+Math.Sqrt(5))/2);
        public static readonly NamedConstantExpr Deg   = new NamedConstantExpr("deg", Math.PI/180);
        public static readonly NamedConstantExpr Reg   = new NamedConstantExpr("rad", 180/Math.PI);
        public static readonly NamedConstantExpr Rpm   = new NamedConstantExpr("rpm", 2*Math.PI/60);
        #endregion

        #region Arithmetic Functions 
        public static Expr Identity(Expr right) => right;
        public static Expr Negate(Expr right)
        {
            if (ArrayExpr.IsVectorizable(right, out int count, out var rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Negate(rightArray[i]);
                }
                return FromArray(vector);
            }

            if (right.Equals(0)) return 0;
            if (right.IsConst(out var val))
            {
                return -val;
            }
            if (right.IsUnary(UnaryOp.Negate, out var arg))
            {
                return arg;
            }
            return new UnaryExpr(UnaryOp.Negate, right);
        }
        public static Expr Add(Expr left, Expr right)
        {
            if (ArrayExpr.IsVectorizable(left, right, out int count, out var leftArray, out var rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Add(leftArray[i], rightArray[i]);
                }
                return FromArray(vector);
            }

            if (left.Equals(0)) return right;
            if (right.Equals(0)) return left;
            if (left.Equals(right)) return 2*left;
            if (left.IsConst(out var lval) && right.IsConst(out var rval))
            {
                return lval+rval;
            }
            if (left.IsFactor(out var lf2, out var lfarg2) && lf2==-1)
            {
                // (-1*x)+y
                return right - lfarg2;
            }
            if (right.IsFactor(out var rf2, out var rfarg2) && rf2==1)
            {
                // x + (-1*y)
                return left - rfarg2;
            }
            Expr argument;
            double lf, rf;
            if (HaveCommonFactor(left, right, out argument, out lf, out rf))
            {
                // (a*x) + (b*x) = (a+b)*x
                return (lf+rf)*argument;
            }
            BinaryOp lop, rop;
            Expr lbLeft, lbRight;
            Expr rbLeft, rbRight;
            if (left.IsBinary(out lop, out lbLeft, out lbRight))
            {                
                switch (lop)
                {
                    case BinaryOp.Add:
                        // (x + y) + z
                        if (HaveCommonFactor(lbLeft, right, out argument, out lf, out rf))
                        {
                            // (a*x + y) + b*x = (a+b)*x + y
                            return (lf+rf)*argument + lbRight;
                        }
                        if (HaveCommonFactor(lbRight, right, out argument, out lf, out rf))
                        {
                            // (x + a*y) + b*y = x + (a+b)*y
                            return lbLeft + (lf+rf)*argument;
                        }
                        break;
                    case BinaryOp.Subtract:
                        // (x - y) + z
                        if (HaveCommonFactor(lbLeft, right, out argument, out lf, out rf))
                        {
                            // (a*x - y) + b*x = (b-a)*x + y
                            return (rf-lf)*argument + lbRight;
                        }
                        if (HaveCommonFactor(lbRight, right, out argument, out lf, out rf))
                        {
                            // (x - a*y) + b*y = x + (b-a)*y
                            return lbLeft + (rf-lf)*argument;
                        }
                        break;
                    case BinaryOp.Multiply:
                        // (x * y) + z
                        if (right.IsBinary(out rop, out rbLeft, out rbRight))
                        {
                            switch (rop)
                            {
                                case BinaryOp.Multiply:
                                    // (x*u) + (y*v)
                                    if (lbLeft.Equals(rbLeft))
                                    {
                                        // (a*u) + (a*v) = a*(u-v)
                                        return lbLeft*(lbRight+rbRight);
                                    }
                                    if (lbLeft.Equals(rbRight))
                                    {
                                        // (a*u) + (y*a) = a*(u-y)
                                        return lbLeft*(lbRight+rbLeft);
                                    }
                                    if (lbRight.Equals(rbRight))
                                    {
                                        // (x*a) + (y*a) = (x-y)*a
                                        return (lbLeft+rbLeft)*rbRight;
                                    }
                                    if (lbRight.Equals(rbLeft))
                                    {
                                        // (x*a) + (a*v) = (x-v)*a
                                        return (lbLeft+rbRight)*lbRight;
                                    }
                                    break;
                            }
                        }
                        break;
                    case BinaryOp.Divide:
                        // (x / y) + z
                        if (right.IsBinary(out rop, out rbLeft, out rbRight))
                        {
                            switch (rop)
                            {
                                case BinaryOp.Divide:
                                    // (x/u) + (y/v)
                                    if (lbRight.Equals(rbRight))
                                    {
                                        // (x/b) + (y/b)
                                        return (lbLeft+rbLeft)/rbRight;
                                    }
                                    if (lbLeft.Equals(rbLeft))
                                    {
                                        // (a/x) + (a/y)
                                        return lbLeft * (1/lbRight + 1/rbRight);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            return new BinaryExpr(BinaryOp.Add, left, right);
        }
        public static Expr Subtract(Expr left, Expr right)
        {
            if (ArrayExpr.IsVectorizable(left, right, out int count, out var leftArray, out var rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Subtract(leftArray[i], rightArray[i]);
                }
                return FromArray(vector);
            }
            if (left.Equals(0)) return -right;
            if (right.Equals(0)) return left;
            if (left.Equals(right)) return 0;
            if (left.IsConst(out var lcValue) && right.IsConst(out var rcValue))
            {
                return lcValue - rcValue;
            }
            if (left.IsFactor(out var lf2, out var lfarg2) && lf2==-1)
            {
                // (-1*x)-y
                return -(lfarg2+right);
            }
            if (right.IsFactor(out var rf2, out var rfarg2) && rf2==1)
            {
                // x - (-1*y)
                return left + rfarg2;
            }
            Expr factor;
            double lcoef, rcoef;
            if (HaveCommonFactor(left, right, out factor, out lcoef, out rcoef))
            {
                // (a*x) - (b*x) = (a-b)*x
                return (lcoef-rcoef)*factor;
            }
            BinaryOp lop, rop;
            Expr lbLeft, lbRight;
            Expr rbLeft, rbRight;
            if (left.IsBinary(out lop, out lbLeft, out lbRight))
            {
                switch (lop)
                {
                    case BinaryOp.Add:
                        // (x + y) - z
                        if (HaveCommonFactor(lbLeft, right, out factor, out lcoef, out rcoef))
                        {
                            // (a*x + y) - b*x = (a-b)*x + y
                            return (lcoef-rcoef)*factor + lbRight;
                        }
                        if (HaveCommonFactor(lbRight, right, out factor, out lcoef, out rcoef))
                        {
                            // (x + a*y) - b*y = x + (a-b)*y
                            return lbLeft + (lcoef-rcoef)*factor;
                        }
                        break;
                    case BinaryOp.Subtract:
                        // (x - y) - z
                        if (HaveCommonFactor(lbLeft, right, out factor, out lcoef, out rcoef))
                        {
                            // (a*x - y) - b*x = (a-b)*x - y
                            return (lcoef-rcoef)*factor - lbRight;
                        }
                        if (HaveCommonFactor(lbRight, right, out factor, out lcoef, out rcoef))
                        {
                            // (x - a*y) - b*y = x - (b+a)*y
                            return lbLeft - (rcoef+lcoef)*factor;
                        }
                        break;
                    case BinaryOp.Multiply:
                        // (x * y) - z
                        if (right.IsBinary(out rop, out rbLeft, out rbRight))
                        {                            
                            switch (rop)
                            {
                                case BinaryOp.Multiply:
                                    // (x*u) - (y*v)
                                    if (lbLeft.Equals(rbLeft))
                                    {
                                        // (a*u) - (a*v) = a*(u-v)
                                        return lbLeft*(lbRight-rbRight);
                                    }
                                    if (lbLeft.Equals(rbRight))
                                    {
                                        // (a*u) - (y*a) = a*(u-y)
                                        return lbLeft*(lbRight-rbLeft);
                                    }
                                    if (lbRight.Equals(rbRight))
                                    {
                                        // (x*a) - (y*a) = (x-y)*a
                                        return (lbLeft-rbLeft)*rbRight;
                                    }
                                    if (lbRight.Equals(rbLeft))
                                    {
                                        // (x*a) - (a*v) = (x-v)*a
                                        return (lbLeft-rbRight)*lbRight;
                                    }
                                    break;
                            }
                        }
                        break;
                    case BinaryOp.Divide:
                        // (x / y) - z
                        if (right.IsBinary(out rop, out rbLeft, out rbRight))
                        {
                            switch (rop)
                            {
                                // (x/u) - (y/v)
                                case BinaryOp.Divide:
                                    if (lbRight.Equals(rbRight))
                                    {
                                        // (x/b) - (y/b) = (x-y)/b
                                        return (lbLeft-rbLeft)/rbRight;
                                    }
                                    if (lbLeft.Equals(rbLeft))
                                    {
                                        // (a/x) - (a/y) = a(1/x-1/y)
                                        return lbLeft * (1/lbRight - 1/rbRight);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            return new BinaryExpr(BinaryOp.Subtract, left, right);
        }
        public static Expr Multiply(Expr left, Expr right)
        {
            if (ArrayExpr.IsVectorizable(left, right, out int count, out var leftArray, out var rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Multiply(leftArray[i], rightArray[i]);
                }
                return FromArray(vector);
            }

            if (left.Equals(0) || right.Equals(0)) return 0;
            if (left.Equals(1)) return right;
            if (right.Equals(1)) return left;
            if (left.Equals(right)) return left ^ 2;
            if (left.IsConst(out var lval) && right.IsConst(out var rval))
            {
                return lval*rval;
            }
            if (left.IsFactor(out var lf, out var lfarg) && right.IsFactor(out var rf, out var rfarg))
            {
                return (lf*rf)*(lfarg*rfarg);
            }
            if (left.IsFactor(out var lf2, out var lfarg2))
            {
                if (right.Equals(lfarg2))
                {
                    return lf2*(right^2);
                }
                if (lf2==-1)
                {
                    return Negate(lfarg*right);
                }
                if (right.IsConst(out double rf3))
                {
                    return (rf3*lf2)*lfarg2;
                }
            }
            if (right.IsFactor(out var rf2, out var rfarg2))
            {
                if (left.Equals(rfarg2))
                {
                    return rf2*(left^2);
                }
                if (rf2==-1)
                {
                    return Negate(left*rfarg2);
                }
                if (left.IsConst(out double rf3))
                {
                    return (rf3*rf2)*rfarg2;
                }
            }
            if (left.IsConst(out var lc2) && Math.Abs(lc2)<1)
            {
                return right/(1/lc2);
            }
            if (right.IsConst(out var rc2) && Math.Abs(rc2)<1)
            {
                return left/(1/rc2);
            }
            if (!right.IsConst(out _) && left.IsBinary(BinaryOp.Divide, out var ldLeft, out var ldRight))
            {
                // (x/y)*z = (x*z)/y
                return (ldLeft * right)/ldRight;
            }
            if (!left.IsConst(out _) && right.IsBinary(BinaryOp.Divide, out var rdLeft, out var rdRight))
            {
                // x*(y/z) = (x*y)/z
                return (left * rdLeft)/rdRight;
            }
            if (left.IsRaised(out var lpBase, out var lpExp) && right.IsRaised(out var rpBase, out var rpExp))
            {
                if (lpBase.Equals(rpBase))
                {
                    // (x^n)*(x^m) = x^(n+m)
                    return Power(lpBase, lpExp+rpExp);
                }
                if (lpExp==rpExp)
                {
                    // (x^n)*(y^n) = (x*y)^n
                    return Power(lpBase*rpBase, lpExp);
                }
            }
            if (AreBinary(left, right, BinaryOp.Divide, out var lbLeft, out var lbRight, out var rbLeft, out var rbRight))
            {
                // (x/u) * (y/v) = (x*y)/(u*v)
                return (lbLeft*rbLeft)/(lbRight*rbRight);
            }
            return new BinaryExpr(BinaryOp.Multiply, left, right);
        }
        public static Expr Divide(Expr left, Expr right)
        {
            if (ArrayExpr.IsVectorizable(left, right, out int count, out var leftArray, out var rightArray))
            {
                var vector = new Expr[count];
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = Divide(leftArray[i], rightArray[i]);
                }
                return FromArray(vector);
            }

            if (left.Equals(0)) return 0;
            if (left.Equals(right)) return 1;
            if (right.Equals(1)) return left;
            if (left.IsConst(out var lval) && right.IsConst(out var rval))
            {
                return lval/rval;
            }
            if (left.IsFactor(out var lf, out var lfarg) && right.IsFactor(out var rf, out var rfarg))
            {
                if (lfarg.Equals(rfarg))
                {
                    return lf/rf;
                }
            }
            if (left.IsFactor(out var lf2, out var lfarg2))
            {
                if (right.Equals(lfarg2))
                {
                    return lf2;
                }
                if (lf2==-1)
                {
                    // (lf*x)/y
                    return lf2*(lfarg/right);
                }
                if (right.IsConst(out double rf3))
                {
                    // (a*x)/b
                    return (lf2/rf3)*lfarg2;
                }

            }
            if (right.IsFactor(out var rf2, out var rfarg2))
            {
                if (left.Equals(rfarg2))
                {
                    return 1/rf2;
                }
                if (rf2==-1)
                {
                    // (x/(rf*y)
                    return (1/rf2)*(left/rfarg2);
                }
                if (left.IsConst(out double rf3))
                {
                    // a/(b*x)
                    return (rf3/rf2)/rfarg2;
                }
            }
            if (left.IsConst(out var lc2))
            {
                return lc2*Inv(right);
            }
            if (right.IsConst(out var rc3) && Math.Abs(rc3)<1)
            {
                return (1/rc3)*left;
            }
            if (left.IsRaised(out var lpBase, out var lpExp) && right.IsRaised(out var rpBase, out var rpExp))
            {
                if (lpBase.Equals(rpBase))
                {
                    // (x^n)/(x^m)
                    return Power(lpBase, lpExp - rpExp);
                }
                if (lpExp==rpExp)
                {
                    // (x^n)/(y^n)
                    return Power(lpBase/rpBase, lpExp);
                }
            }
            if (left.IsBinary(BinaryOp.Divide, out var ldLeft, out var ldRight))
            {
                // (x/y)/z = x/(y*x)
                return (ldLeft * right)/ldRight;
            }
            if (right.IsBinary(BinaryOp.Divide, out var rdLeft, out var rdRight))
            {
                // x/(y/z) = (x*z)/y
                return (left * rdRight)/rdLeft;
            }
            if (AreBinary(left, right, BinaryOp.Divide, out var lbLeft, out var lbRight, out var rbLeft, out var rbRight))
            {
                // (x/u) / (y/v) = (x*v)/(u*y)
                return (lbLeft*rbRight)/(lbRight*rbLeft);
            }
            return new BinaryExpr(BinaryOp.Divide, left, right);
        }
        #endregion

        #region Math Operators
        public static Expr operator +(Expr rhs) => Identity(rhs);
        public static Expr operator -(Expr rhs) => Negate(rhs);
        public static Expr operator +(Expr lhs, Expr rhs) => Add(lhs, rhs);
        public static Expr operator -(Expr lhs, Expr rhs) => Subtract(lhs, rhs);
        public static Expr operator *(Expr lhs, Expr rhs) => Multiply(lhs, rhs);
        public static Expr operator /(double lhs, Expr rhs) => Divide(lhs, rhs);
        public static Expr operator /(Expr lhs, Expr rhs) => Divide(lhs, rhs);
        public static Expr operator ^(Expr lhs, double rhs) => Raise(lhs, rhs);
        public static Expr operator ^(Expr lhs, Expr rhs) => Power(lhs, rhs);
        #endregion

        #region Unary Functions
        public static Expr Random(Expr factor)
        {
            return new UnaryExpr(UnaryOp.Rnd, factor);
        }

        public static Expr Pi(Expr factor)
        {
            if (factor.IsFactor(out double f, out Expr argument))
            {
                return (Math.PI*f)*argument;
            }
            return new UnaryExpr(UnaryOp.Pi, factor);
        }

        public static Expr Abs(Expr right)
        {
            if (right.IsUnary(UnaryOp.Sqr, out _))
            {
                return right;
            }
            if (right.IsUnary(UnaryOp.Sqrt, out _))
            {
                return Sqrt(Abs(right));
            }
            return new UnaryExpr(UnaryOp.Abs, right);
        }

        public static Expr Sign(Expr right)
        {
            Expr argument;
            if (right.IsUnary(UnaryOp.Sign, out argument))
            {
                return right;
            }
            if (right.IsUnary(UnaryOp.Sqr, out _))
            {
                return 1;
            }
            if (right.IsUnary(UnaryOp.Sqrt, out _))
            {
                return 1;
            }
            if (right.IsFactor(out double factor, out argument))
            {
                if (Sign(argument).IsConst(out double s))
                {
                    return Math.Sign(factor*s);
                }
            }
            return new UnaryExpr(UnaryOp.Sign, right);
        }

        public static Expr Exp(Expr right)
        {
            if (right.IsUnary(UnaryOp.Log, out var argument))
            {
                return argument;
            }
            if (right.IsBinary(BinaryOp.Log, out var rbLeft, out var rbRight))
            {
                // Exp(Log(x,y)) = x^(1/Ln(y))
                return Power(rbLeft, 1/Log(rbRight));
            }
            return new UnaryExpr(UnaryOp.Exp, right);
        }

        public static Expr Log(Expr right)
        {
            if (right.IsUnary(UnaryOp.Exp, out var argument))
            {
                return argument;
            }
            if (right.IsBinary(BinaryOp.Pow, out var rbLeft, out var rbRight))
            {
                // Log(x^y) = y*Log(x)
                return rbRight*Log(rbLeft);
            }
            return new UnaryExpr(UnaryOp.Log, right);
        }

        public static Expr Log2(Expr right)
        {
            if (right.IsBinary(BinaryOp.Pow, out var rbLeft, out var rbRight))
            {
                // Log2(x^y) = y*Log2(x)
                return rbRight*Log2(rbLeft);
            }
            return new UnaryExpr(UnaryOp.Log2, right);
        }

        public static Expr Log10(Expr right)
        {
            if (right.IsBinary(BinaryOp.Pow, out var rbLeft, out var rbRight))
            {
                // Log10(x^y) = y*Log10(x)
                return rbRight*Log10(rbLeft);
            }
            return new UnaryExpr(UnaryOp.Log10, right);
        }

        public static Expr Sqr(Expr right)
        {
            if (right.IsUnary(UnaryOp.Sqrt, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Sqr, right);
        }

        public static Expr Cub(Expr right)
        {
            if (right.IsUnary(UnaryOp.Cbrt, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Cub, right);
        }

        public static Expr Sqrt(Expr right)
        {
            if (right.IsUnary(UnaryOp.Sqr, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Sqrt, right);
        }

        public static Expr Cbrt(Expr right)
        {
            if (right.IsUnary(UnaryOp.Cub, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Cbrt, right);
        }

        public static Expr Floor(Expr right)
        {
            if (right.IsConst(out double value))
            {
                return Math.Floor(value);
            }
            return new UnaryExpr(UnaryOp.Floor, right);
        }

        public static Expr Ceiling(Expr right)
        {
            if (right.IsConst(out double value))
            {
                return Math.Ceiling(value);
            }
            return new UnaryExpr(UnaryOp.Ceiling, right);
        }

        public static Expr Round(Expr right)
        {
            if (right.IsConst(out double value))
            {
                return Math.Round(value);
            }
            return new UnaryExpr(UnaryOp.Round, right);
        }

        public static Expr Inv(Expr right)
        {
            if (right.IsUnary(UnaryOp.Inverse, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Inverse, right);
        }

        public static Expr Raise(Expr left, double right)
        {
            if (right==0) return 1;
            if (right==1) return left;
            if (right==2) return Sqr(left);
            if (right==3) return Cub(left);
            if (right==-1) return 1/left;
            if (right==-2) return 1/Sqr(left);
            if (right==-3) return 1/Cub(left);
            if (right==1/2.0) return Sqrt(left);
            if (right==1/3.0) return Cbrt(left);
            if (right==-1/2.0) return 1/Sqrt(left);
            if (right==-1/3.0) return 1/Cbrt(left);
            if (left.IsConst(out var lc))
            {
                return Math.Pow(lc, right);
            }
            return new BinaryExpr(BinaryOp.Pow, left, right);
        }
        #endregion

        #region Binary Functions

        public static Expr Min(Expr left, Expr right)
        {
            return new BinaryExpr(BinaryOp.Min, left, right);
        }

        public static Expr Max(Expr left, Expr right)
        {
            return new BinaryExpr(BinaryOp.Max, left, right);
        }

        public static Expr Sign(Expr magnitude, Expr function)
        {
            if (magnitude.IsConst(out var mag))
            {
                return Math.Abs(mag)*Sign(function);
            }
            return new BinaryExpr(BinaryOp.Sign, magnitude, function);
        }

        public static Expr Power(Expr left, Expr right)
        {
            if (right.Equals(0)) return 1;
            if (right.Equals(1)) return left;
            if (left.IsConst(out var lc1) && right.IsConst(out var rc1))
            {
                return Math.Pow(lc1, rc1);
            }
            if (right.IsConst(out var rc2))
            {
                return Raise(left, rc2);
            }            
            return new BinaryExpr(BinaryOp.Pow, left, right);
        }
        public static Expr Log(Expr argument, Expr exponent)
        {
            return new BinaryExpr(BinaryOp.Log, argument, exponent);
        }
        #endregion

        #region Trigonometry
        public static Expr Sin(Expr right)
        {
            if (right.IsUnary(UnaryOp.Asin, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Sin, right);
        }

        public static Expr Cos(Expr right)
        {
            if (right.IsUnary(UnaryOp.Acos, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Cos, right);
        }

        public static Expr Tan(Expr right)
        {
            if (right.IsUnary(UnaryOp.Atan, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Tan, right);
        }

        public static Expr Sinh(Expr right)
        {
            if (right.IsUnary(UnaryOp.Asinh, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Sinh, right);
        }

        public static Expr Cosh(Expr right)
        {
            if (right.IsUnary(UnaryOp.Acosh, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Cosh, right);
        }

        public static Expr Tanh(Expr right)
        {
            if (right.IsUnary(UnaryOp.Atanh, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Tanh, right);
        }

        public static Expr Asin(Expr right)
        {
            if (right.IsUnary(UnaryOp.Sin, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Asin, right);
        }

        public static Expr Acos(Expr right)
        {
            if (right.IsUnary(UnaryOp.Cos, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Acos, right);
        }

        public static Expr Atan(Expr right)
        {
            if (right.IsUnary(UnaryOp.Tan, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Atan, right);
        }

        public static Expr Asinh(Expr right)
        {
            if (right.IsUnary(UnaryOp.Sinh, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Asinh, right);
        }

        public static Expr Acosh(Expr right)
        {
            if (right.IsUnary(UnaryOp.Cosh, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Acosh, right);
        }

        public static Expr Atanh(Expr right)
        {
            if (right.IsUnary(UnaryOp.Tanh, out var argument))
            {
                return argument;
            }
            return new UnaryExpr(UnaryOp.Atanh, right);
        }
        #endregion

        #region String Handling
        public override sealed string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
#pragma warning disable S927 // Parameter names should match base declaration and other partial definitions
        public abstract string ToString(string format, IFormatProvider provider);
#pragma warning restore S927 // Parameter names should match base declaration and other partial definitions
        #endregion

        #region IEquatable Members
        /// <summary>
        /// Checks for equality among <see cref="Expr"/> classes
        /// </summary>
        /// <returns>True if equal</returns>
        public abstract bool Equals(Expr other);

        /// <summary>
        /// Equality overrides from <see cref="System.Object"/>
        /// </summary>
        /// <param name="obj">The object to compare this with</param>
        /// <returns>False if object is a different type, otherwise it calls <code>Equals(Expr)</code></returns>
        public override bool Equals(object obj)
        {
            if (obj is Expr item)
            {
                return Equals(item);
            }
            if (obj is double x)
            {
                return Equals(Number(x));
            }
            if (obj is string sym)
            {
                return Equals(VariableOrConst(sym));
            }
            return false;
        }

        /// <summary>
        /// Calculates the hash code for the <see cref="Expr"/>
        /// </summary>
        /// <returns>The int hash value</returns>
        public override abstract int GetHashCode();

        #endregion

    }

}
