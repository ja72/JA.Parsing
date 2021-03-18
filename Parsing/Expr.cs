﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using static System.Math;

namespace JA.Parsing
{
    public abstract class Expr : IFormattable, IEquatable<Expr>
    {
        public static IReadOnlyCollection<(string sym, double val)> Constants
            => new ReadOnlyCollection<(string sym, double val)>(NamedConstantExpr.Defined.Select((item) => (item.Key, item.Value.Value)).ToList());

        public static implicit operator Expr(double x) => Number(x);
        public static implicit operator Expr(string expr) => Parse(expr);
        public static double operator |(Expr expr, (string sym, double val) arg)
        {
            return expr.Eval(arg);
        }

        #region Convenience Helpers

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

        #endregion


        public Func<double> GetFunction() => () => Eval();
        public Func<double, double> GetFunction(string var1) => (x) => Eval((var1, x));
        public Func<double, double, double> GetFunction(string var1, string var2) => (x,y) => Eval((var1,x),(var2,y));

        public double this[params (string sym, double val)[] parameters]
        {
            get => Eval(parameters);
        }
        /// <summary>
        /// Evaluates the expression with specified variable,value pairs
        /// </summary>
        /// <param name="parameters">The list of variable values.</param>
        public abstract double Eval(params (string sym, double val)[] parameters);
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
        public abstract Expr Substitute(Expr target, Expr expression);
        public Expr Substitute(string symbol, Expr expression) => Substitute(Variable(symbol), expression);
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
            return new VariableExpr(symbol);
        }

        public static readonly ConstExpr Zero = Number(0);
        public static readonly ConstExpr One = Number(1);
        public static readonly NamedConstantExpr Inf   = new NamedConstantExpr("inf", double.PositiveInfinity);
        public static readonly NamedConstantExpr Nan   = new NamedConstantExpr("nan", double.NaN);
        public static readonly NamedConstantExpr PI    = new NamedConstantExpr("pi", Math.PI);
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
            if (right.Equals(0)) return 0;
            if (right is ConstExpr rc)
            {
                return -rc.Value;
            }
            if (right is UnaryOperatorExpr rn && rn.Op=="-")
            {
                return rn.Argument;
            }
            return new UnaryOperatorExpr("-", right);
        }
        public static Expr Add(Expr left, Expr right)
        {
            if (left.Equals(0)) return right;
            if (right.Equals(0)) return left;            
            if (left.Equals(right)) return 2*left;
            if (left is ConstExpr lc && right is ConstExpr rc)
            {
                return lc.Value + rc.Value;
            }
            if (right is UnaryOperatorExpr rn && rn.Op=="-")
            {
                return left - rn.Argument;
            }
            if (left is UnaryOperatorExpr ln && ln.Op=="-")
            {
                return right - ln.Argument;
            }
            if (left is BinaryOperatorExpr lm && lm.Op=="*" 
                && right is BinaryOperatorExpr rm && rm.Op=="*")
            {
                if (lm.Left.Equals(rm.Left))
                {
                    // (x*y)+(x*z) = x*(y+z)
                    return lm.Left*(lm.Right+rm.Right);
                }
                if (lm.Right.Equals(rm.Right))
                {
                    // (x*y)+(z*y) = (x+z)*y
                    return (lm.Left+rm.Left)*lm.Right;
                }
                if (lm.Left.Equals(rm.Right))
                {
                    // (x*y)+(z*x) = x*(y+z)
                    return lm.Left*(lm.Right+rm.Left);
                }
                if (lm.Right.Equals(rm.Left))
                {
                    // (x*y)+(y*z) = (x+z)*y
                    return (lm.Left+rm.Right)*lm.Right;
                }
            }
            if (left is BinaryOperatorExpr ld && ld.Op=="/"
                && right is BinaryOperatorExpr rd && rd.Op=="/")
            {
                if (ld.Right.Equals(rd.Right))
                {
                    // (x/y)+(z/y) = (x+z)/y
                    return (ld.Left+rd.Left)/ld.Right;
                }
            }
            return new BinaryOperatorExpr("+", left, right);
        }
        public static Expr Subtract(Expr left, Expr right)
        {
            if (left.Equals(0)) return -right;
            if (right.Equals(0)) return left;
            if (left.Equals(right)) return 0;
            if (left is ConstExpr lc && right is ConstExpr rc)
            {
                return lc.Value - rc.Value;
            }
            if (right is UnaryOperatorExpr rn && rn.Op=="-")
            {
                return left + rn.Argument;
            }
            if (left is UnaryOperatorExpr ln && ln.Op=="-")
            {
                return -( ln.Argument + right);
            }
            if (left is BinaryOperatorExpr lm && lm.Op=="*"
                && right is BinaryOperatorExpr rm && rm.Op=="*")
            {
                if (lm.Left.Equals(rm.Left))
                {
                    // (x*y)-(x*z) = x*(y-z)
                    return lm.Left*(lm.Right-rm.Right);
                }
                if (lm.Right.Equals(rm.Right))
                {
                    // (x*y)-(z*y) = (x-z)*y
                    return (lm.Left-rm.Left)*lm.Right;
                }
                if (lm.Left.Equals(rm.Right))
                {
                    // (x*y)-(z*x) = x*(y-z)
                    return lm.Left*(lm.Right-rm.Left);
                }
                if (lm.Right.Equals(rm.Left))
                {
                    // (x*y)-(y*z) = (x-z)*y
                    return (lm.Left-rm.Right)*lm.Right;
                }
            }
            if (left is BinaryOperatorExpr ld && ld.Op=="/"
                && right is BinaryOperatorExpr rd && rd.Op=="/")
            {
                if (ld.Right.Equals(rd.Right))
                {
                    // (x/y)-(z/y) = (x-z)/y
                    return (ld.Left-rd.Left)/ld.Right;
                }
            }
            return new BinaryOperatorExpr("-", left, right);
        }
        public static Expr Multiply(Expr left, Expr right)
        {
            if (left.Equals(0) || right.Equals(0)) return 0;
            if (left.Equals(1)) return right;
            if (right.Equals(1)) return left;
            if (left.Equals(right)) return left ^ 2;
            if (left is ConstExpr lc && right is ConstExpr rc)
            {
                return lc.Value * rc.Value;
            }
            if (left is ConstExpr lc2 && Math.Abs(lc2.Value)<1)
            {
                return right/(1/lc2.Value);
            }
            if (right is ConstExpr rc2 && Math.Abs(rc2.Value)<1)
            {
                return left/(1/rc2.Value);
            }
            if (right is UnaryOperatorExpr rn && rn.Op=="-")
            {
                // x*(-y) = -(x*y)
                return -(left * rn.Argument);
            }
            if (left is UnaryOperatorExpr ln && ln.Op=="-")
            {
                // (-x)*y = -(x*y)
                return -(ln.Argument * right);
            }
            if (left is BinaryOperatorExpr ld && ld.Op=="/")
            {
                // (x/y)*z = (x*z)/y
                return (ld.Left * right)/ld.Right;
            }
            if (right is BinaryOperatorExpr rd && rd.Op=="/")
            {
                // x*(y/z) = (x*y)/z
                return (left * rd.Left)/rd.Right;
            }
            if (left is BinaryFunctionExpr lp && lp.Name=="pow"
                && right is BinaryFunctionExpr rp && rp.Name=="pow")
            {
                if (lp.Left.Equals(rp.Left))
                {
                    // (x^n)*(x^m) = x^(n+m)
                    return Power(lp.Left, lp.Right+rp.Right);
                }
            }
            return new BinaryOperatorExpr("*", left, right);
        }
        public static Expr Divide(Expr left, Expr right)
        {
            if (left.Equals(0)) return 0;
            if (left.Equals(right)) return 1;
            if (right.Equals(1)) return left;
            if (left is ConstExpr lc1 && right is ConstExpr rc1)
            {
                return lc1.Value/rc1.Value;
            }
            if (left is ConstExpr lc3 && Math.Abs(lc3.Value)<1)
            {
                return 1/((1/lc3.Value)*right);
            }
            if (right is ConstExpr rc2 && Math.Abs(rc2.Value)<1)
            {
                return (1/rc2.Value)*left;
            }
            if (left is UnaryFunctionExpr ls && ls.Name=="sqr")
            {                
                if (right.Equals(ls.Argument))
                {
                    // x^2/x = x
                    return right;
                }
            }
            if (right is UnaryFunctionExpr rs && rs.Name=="sqr")
            {
                if (left.Equals(rs.Argument))
                {
                    // x/x^2 = 1/x
                    return 1/left;
                }
            }
            if (right is UnaryOperatorExpr rn && rn.Op=="-")
            {
                // x/(-y) = -(x/y)
                return -(left/rn.Argument);
            }
            if (left is UnaryOperatorExpr ln && ln.Op=="-")
            {
                // (-x)/y = -(x/y)
                return -(ln.Argument/right);
            }
            if (right is UnaryFunctionExpr ri && ri.Name=="inv")
            {
                // x/(1/y) = x*y
                return left * ri.Argument;
            }
            if (left is UnaryFunctionExpr li && li.Name=="inv")
            {
                // (1/x)/y = 1/(x*y)
                return 1/(li.Argument * right);
            }
            if (left is BinaryOperatorExpr ld && ld.Op=="/")
            {
                // (x/y)/z = x/(y*x)
                return (ld.Left * right)/ld.Right;
            }
            if (right is BinaryOperatorExpr rd && rd.Op=="/")
            {
                // x/(y/z) = (x*z)/y
                return (left * rd.Right)/rd.Left;
            }
            if (left is BinaryFunctionExpr lp && lp.Name=="pow"
                && right is BinaryFunctionExpr rp && rp.Name=="pow")
            {
                if (lp.Left.Equals(rp.Left))
                {
                    // (x^n)/(x^m) = x^(n-m)
                    return Power(lp.Left, lp.Right-rp.Right);
                }
            }
            return new BinaryOperatorExpr("/", left, right);
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
            if (left is ConstExpr lc)
            {
                return Math.Pow(lc.Value, right);
            }
            return new BinaryFunctionExpr("pow", left, right);
        }
        public static Expr Power(Expr left, Expr right)
        {
            if (right.Equals(0)) return 1;
            if (right.Equals(1)) return left;
            if (right is ConstExpr rc2)
            {
                return Raise(left, rc2.Value);
            }
            if (left is ConstExpr lc1 && right is ConstExpr rc1)
            {
                return Math.Pow(lc1.Value, rc1.Value);
            }
            return new BinaryFunctionExpr("pow", left, right);
        } 
        #endregion

        #region Math Operators
        public static Expr operator +(Expr rhs) => Identity(rhs);
        public static Expr operator -(Expr rhs) => Negate(rhs);
        public static Expr operator +(Expr lhs, Expr rhs) => Add(lhs, rhs);
        public static Expr operator -(Expr lhs, Expr rhs) => Subtract(lhs, rhs);
        public static Expr operator *(Expr lhs, Expr rhs) => Multiply(lhs, rhs);
        public static Expr operator /(double lhs, Expr rhs) => Divide(lhs,rhs);
        public static Expr operator /(Expr lhs, Expr rhs) => Divide(lhs, rhs);
        public static Expr operator ^(Expr lhs, double rhs) => Raise(lhs, rhs);
        public static Expr operator ^(Expr lhs, Expr rhs) => Power(lhs, rhs);
        #endregion

        #region Unary Functions
        public static Expr Abs(Expr right) => new UnaryFunctionExpr("abs", right);
        public static Expr Sign(Expr right) => new UnaryFunctionExpr("sign", right);
        public static Expr Exp(Expr right) => new UnaryFunctionExpr("exp", right);
        public static Expr Ln(Expr right) => new UnaryFunctionExpr("ln", right);
        public static Expr Sqr(Expr right) => new UnaryFunctionExpr("sqr", right);
        public static Expr Cub(Expr right) => new UnaryFunctionExpr("cub", right);
        public static Expr Sqrt(Expr right) => new UnaryFunctionExpr("sqrt", right);
        public static Expr Cbrt(Expr right) => new UnaryFunctionExpr("cbrt", right);
        public static Expr Floor(Expr right) => new UnaryFunctionExpr("floor", right);
        public static Expr Ceil(Expr right) => new UnaryFunctionExpr("ceil", right);
        public static Expr Round(Expr right) => new UnaryFunctionExpr("round", right);
        #endregion

        #region Trigonometry
        public static Expr Sin(Expr right) => new UnaryFunctionExpr("sin", right);
        public static Expr Cos(Expr right) => new UnaryFunctionExpr("cos", right);
        public static Expr Tan(Expr right) => new UnaryFunctionExpr("tan", right);
        public static Expr Sinh(Expr right) => new UnaryFunctionExpr("sinh", right);
        public static Expr Cosh(Expr right) => new UnaryFunctionExpr("cosh", right);
        public static Expr Tanh(Expr right) => new UnaryFunctionExpr("tanh", right);
        public static Expr Asin(Expr right) => new UnaryFunctionExpr("asin", right);
        public static Expr Acos(Expr right) => new UnaryFunctionExpr("acos", right);
        public static Expr Atan(Expr right) => new UnaryFunctionExpr("atan", right);
        public static Expr Asinh(Expr right) => new UnaryFunctionExpr("asinh", right);
        public static Expr Acosh(Expr right) => new UnaryFunctionExpr("acosh", right);
        public static Expr Atanh(Expr right) => new UnaryFunctionExpr("atanh", right);
        #endregion

        #region Auxiliary Functions
        public static Expr Inv(Expr right) => 1/right;
        #endregion

        #region String Handling
        public override sealed string ToString()
            => ToString("g");
        public string ToString(string formatting)
            => ToString(formatting, null);
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
