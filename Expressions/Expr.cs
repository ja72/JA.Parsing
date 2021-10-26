using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{
    using System.Diagnostics;
    using System.Xml.Linq;
    using JA.Expressions.Parsing;

    public abstract record Expr : IExpression<Expr>
    {
        #region State
        public static readonly Dictionary<string, double> Variables = new();
        #endregion

        public abstract int Rank { get; }
        protected internal abstract Expr Substitute(Expr variable, Expr value);
        public abstract IQuantity Eval(params (string sym, double val)[] parameters);
        protected internal abstract void Compile(ILGenerator gen, Dictionary<string, int> env);
        void IExpression.Compile(ILGenerator gen, Dictionary<string, int> env) => Compile(gen, env);
        protected internal abstract void FillSymbols(ref List<string> variables);
        protected internal abstract void FillValues(ref List<double> values);


        #region Calculus
        /// <summary>
        /// Get the partial derivative of the expression with respect to a variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// </example>
        public abstract Expr PartialDerivative(SymbolExpr variable);
        public Expr PartialDerivative(string symbol) => PartialDerivative(Variable(symbol));
        public Expr PartialDerivative(Expr expr)
        {
            if (expr.IsSymbol(out string sym))
            {
                return PartialDerivative(sym);
            }
            throw new NotSupportedException("Cannot take partial derivative with expression.");
        }
        public Expr Jacobian() => Jacobian(GetSymbols());
        public Expr Jacobian(params string[] symbols)
        {
            var array = new Expr[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                array[i] = PartialDerivative(symbols[i]);
            }
            return Array(array);
        }
        #endregion

        #region Methods
        internal Expr TotalDerivative(ref List<string> paramsAndDots) 
        {
            var @params = paramsAndDots.Select( 
                (item)=> ( item, (Expr)Variable(item).Dot() )).ToArray();
            return TotalDerivative(@params, ref paramsAndDots);
        }
        internal Expr TotalDerivative((string sym, Expr expr)[] parameters, ref List<string> paramsAndDots)
        {
            Expr body = 0;
            foreach (var (sym, expr) in parameters)
            {
                var q_dot = expr;
                if (q_dot.IsSymbol(out var sym_dot))
                {
                    paramsAndDots.Add(sym_dot);
                }
                body += this.PartialDerivative(sym) * q_dot;
            }
            return body;
        }
        public Function GetFunction(string name, bool alphabetically = false)
        {
            return GetFunction(name, GetSymbols(alphabetically));
        }
        public Function GetFunction(string name, params string[] arguments)
        {
            return new Function(name, this, arguments);
        }
        public Function GetFunction(string name, string[] arguments, (string sym, double val)[] knownValues)
        {
            var expr = Substitute(knownValues);
            return new Function(name, expr, arguments);
        }
        public Expr Substitute(params (string sym, double val)[] knownValues)
        {
            Expr result = this;
            foreach (var (sym, val) in knownValues)
            {
                result = result.Substitute(sym, val);
            }
            return result;
        }
        public Expr Substitute(params (string sym, Expr expr)[] subExpressions)
        {
            Expr result = this;
            foreach (var (sym, expr) in subExpressions)
            {
                result = result.Substitute(sym, expr);
            }
            return result;
        }

        public string[] GetSymbols(bool alphabetically = true)
        {
            var all = new List<string>();
            FillSymbols(ref all);
            var list = all.Distinct();
            if (alphabetically)
            {
                list = list.OrderBy((x) => x);
            }
            return list.ToArray();
        }
        public double[] GetValues()
        {
            var list = new List<double>();
            FillValues(ref list);
            return list.ToArray();
        }
        #endregion

        #region Factory
        public static implicit operator Expr(string symbol) => Variable(symbol);
        public static implicit operator Expr(double value) => Const(value);
        //public static implicit operator Expr(Expr[] array) => Array(array);
        public static implicit operator Expr(Vector vector) => Array(vector.Elements);
        //public static implicit operator Expr(Expr[][] matrix) => Matrix(matrix);
        public static implicit operator Expr(Matrix matrix) => Matrix(matrix.Elements);

        public static explicit operator string(Expr expression)
        {
            if (expression.IsSymbol(out string symbol))
            {
                return symbol;
            }
            throw new ArgumentException("Argument must be a " + nameof(SymbolExpr) + " type.", nameof(expression));
        }
        public static explicit operator double(Expr expression)
        {
            if (expression.IsConstant(out double value))
            {
                return value;
            }
            throw new ArgumentException("Argument must be a " + nameof(ValueExpr) + " type.", nameof(expression));
        }

        public static ValueExpr Const(double value)
        {
            var result = new ValueExpr(value);
            if (result.IsNamedConstant(out string symbol, out value))
            {
                return Const(symbol, value);
            }
            return result;
        }

        public static NamedValueExpression Const(string name, double value) => new(name, value);
        public static SymbolExpr Variable(string name) => new(name);

        public static Expr Unary(UnaryOp Op, Expr Argument)
        {
            if (Argument.IsConstant(out double value))
            {
                if (!Argument.IsNamedConstant(out _, out _))
                {
                    return Op.Function(value);
                }
            }
            if (Op.Identifier=="+") return Argument;
            if (Argument.IsUnary(out string argOp, out Expr argArg))
            {
                if (Op.Identifier=="-" && argOp=="-") return argArg;
                if (Op.Identifier=="inv" && argOp=="inv") return argArg;
                if (Op.Identifier=="log" && argOp=="exp") return argArg;
                if (Op.Identifier=="exp" && argOp=="log") return argArg;
                if (Op.Identifier=="sqrt" && argOp=="sqr") return Abs(argArg);
                if (Op.Identifier=="cbrt" && argOp=="cub") return argArg;
                if (Op.Identifier=="cub" && argOp=="cbrt") return argArg;
                if (Op.Identifier=="sin" && argOp=="asin") return argArg;
                if (Op.Identifier=="cos" && argOp=="acos") return argArg;
                if (Op.Identifier=="tan" && argOp=="atan") return argArg;
                if (Op.Identifier=="asin" && argOp=="sin") return argArg;
                if (Op.Identifier=="acos" && argOp=="cos") return argArg;
                if (Op.Identifier=="atan" && argOp=="tan") return argArg;
                if (Op.Identifier=="sinh" && argOp=="asinh") return argArg;
                if (Op.Identifier=="cosh" && argOp=="acosh") return argArg;
                if (Op.Identifier=="tanh" && argOp=="atanh") return argArg;
                if (Op.Identifier=="asinh" && argOp=="sinh") return argArg;
                if (Op.Identifier=="acosh" && argOp=="cosh") return argArg;
                if (Op.Identifier=="atanh" && argOp=="tanh") return argArg;
            }
            return new UnaryExpr(Op, Argument);
        }
        public static Expr Binary(BinaryOp Op, Expr Left, Expr Right)
        {
            if (Left.IsConstant(out var leftValue) && Right.IsConstant(out var rightValue))
            {
                if (Left.IsNamedConstant(out _, out _) | Right.IsNamedConstant(out _, out _))
                {
                    return new BinaryExpr(Op, Left, Right);
                }
                return Op.Function(leftValue, rightValue);
            }
            return new BinaryExpr(Op, Left, Right);
        }

        public static Expr Array(double[] elements)
            => Array(elements.Select((x) => Const(x)));

        public static Expr Array(IEnumerable<Expr> list)
        {
            return Array(list.ToArray());
        }

        public static Expr Array(params Expr[] array)
        {
            if (array.Length==0)
            {
                return 0;
            }
            if (array.Length==1)
            {
                return array[0];
            }
            for (int k = 0; k < array.Length; k++)
            {
                if (array[k].IsArray(out var row))
                {
                    int cols = row.Length;
                    var matrix = new Expr[array.Length][];
                    for (int i = 0; i < matrix.Length; i++)
                    {
                        if (array[i].IsArray(out row))
                        {
                            matrix[i] = row;
                        }
                        else
                        {
                            row = new Expr[cols];
                            row[i] = array[i];
                            matrix[i] = row;
                        }
                    }
                    return Matrix(matrix);
                }
            }
            return new ArrayExpr(array);
        }

        public static Expr Matrix(double[][] matrix)
        {
            var expr = new Expr[matrix.Length][];
            for (int i = 0; i < expr.Length; i++)
            {
                expr[i] = matrix[i].Select((item) => Const(item)).ToArray();
            }
            return Matrix(expr);
        }

        public static Expr Matrix(Expr[][] matrix)
        {
            if (matrix.Length==0)
            {
                return 0;
            }
            if (matrix.Length==1)
            {
                return Array(matrix[0]);
            }
            return new ArrayExpr(matrix);
        }

        public static Expr ArrayIndex(Expr array, int index)
        {
            if (array.IsArray(out var elements))
            {
                return elements[index];
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public static Expr Sum(params Expr[] terms)
        {
            List<double> constants = new();
            List<Expr> expressions = new();
            for (int i = 0; i < terms.Length; i++)
            {
                if (terms[i].IsConstant(out double x))
                {
                    constants.Add(x);
                }
                else
                {
                    expressions.Add(terms[i]);
                }
            }
            List<Expr> collect = new();
            foreach (var grp in expressions.GroupBy((item) => item))
            {
                collect.Add(grp.Count() * grp.Key);
            }
            return collect.Aggregate((sum, expr) => sum+expr) + constants.Sum();
        }
        public static Expr Sum(Expr array)
        {
            if (array.IsArray(out var terms))
            {
                return Sum(terms);
            }
            return array;
        }
        #endregion

        #region Classification
        public bool IsConstant(out double value)
        {
            if (this is ValueExpr vExpr)
            {
                value = vExpr.Value;
                return true;
            }
            value = 0;
            return false;
        }
        public bool IsNamedConstant(out string symbol, out double value)
        {
            if (this is NamedValueExpression namedExpr && !string.IsNullOrEmpty(namedExpr.Name))
            {
                symbol = namedExpr.Name;
                value = namedExpr.Value;
                return true;
            }
            if (this is ValueExpr valExpr)
            {
                int index = System.Array.FindIndex(
                    KnownConstDictionary.Defined.ToArray(),
                    (k)=> k.Value == valExpr.Value);
                if (index>=0)
                {
                    var op = KnownConstDictionary.Defined[index];
                    symbol = op.Identifier;
                    value = op.Value;
                    return true;
                }
            }
            symbol = string.Empty;
            value = 0;
            return false;
        }
        public bool IsNamedConstant(string symbol, out double value)
        {
            if (this is NamedValueExpression namedExpr)
            {
                if (namedExpr.Name == symbol)
                {
                    value = namedExpr.Value;
                    return true;
                }
            }
            value = 0;
            return false;
        }
        public bool IsSymbol(out string symbol)
        {
            if (this is SymbolExpr symExpr)
            {
                symbol = symExpr.Name;
                return true;
            }
            symbol = string.Empty;
            return false;
        }
        public bool IsSymbol(string symbol)
        {
            if (this is SymbolExpr symExpr && symExpr.Name == symbol)
            {
                return true;
            }
            return false;
        }
        public bool IsUnary(out string operation, out Expr argument)
        {
            if (this is UnaryExpr unaryExpr)
            {
                operation = unaryExpr.Op.Identifier;
                argument = unaryExpr.Argument;
                return true;
            }
            operation = string.Empty;
            argument = null;
            return false;
        }
        public bool IsUnary(string operation, out Expr argument)
        {
            if (this is UnaryExpr unaryExpr && unaryExpr.Op.Identifier == operation)
            {
                argument = unaryExpr.Argument;
                return true;
            }
            argument = null;
            return false;
        }
        public bool IsBinary(out string operation, out Expr left, out Expr right)
        {
            if (this is BinaryExpr binaryExpr)
            {
                operation = binaryExpr.Op.Identifier;
                left = binaryExpr.Left;
                right = binaryExpr.Right;
                return true;
            }
            operation = string.Empty;
            left = null;
            right = null;
            return false;
        }
        public bool IsBinary(string operation, out Expr left, out Expr right)
        {
            if (this is BinaryExpr binaryExpr && binaryExpr.Op.Identifier == operation)
            {
                left = binaryExpr.Left;
                right = binaryExpr.Right;
                return true;
            }
            left = null;
            right = null;
            return false;
        }
        public bool IsFactor(out double factor, out Expr argument)
        {
            if (IsBinary("*", out var mul_left, out var mul_arg))
            {
                if (mul_left.IsConstant(out factor))
                {
                    argument = mul_arg;
                    return true;
                }
            }
            if (IsBinary("/", out var div_left, out var div_arg))
            {
                if (div_left.IsConstant(out factor))
                {
                    argument = 1/div_arg;
                    return true;
                }
            }
            if (IsUnary("-", out var neg_arg))
            {
                factor = -1;
                argument= neg_arg;
                return true;
            }
            if (IsUnary("+", out var pos_arg))
            {
                factor = 1;
                argument= pos_arg;
                return true;
            }
            if (IsUnary(out var _, out var _))
            {
                factor = 1;
                argument= this;
                return true;
            }
            factor = 0;
            argument = null;
            return false;
        }

        public bool IsArray(out Expr[] elements)
        {
            if (this is ArrayExpr arrayExpr)
            {
                elements = arrayExpr.Elements;
                return true;
            }
            elements = null;
            return false;
        }
        public bool IsMatrix(out Expr[][] elements)
        {
            if (IsArray(out var rows))
            {
                elements = new Expr[rows.Length][];
                for (int i = 0; i < elements.Length; i++)
                {
                    if (rows[i].IsArray(out var items))
                    {
                        elements[i] = items;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            elements = null;
            return false;
        }

        #endregion

        #region Transformations
        public Expr Simplify()
        {
            Expr prev;
            Expr result = this;
            do
            {
                prev = result;

                switch (this)
                {
                    case UnaryExpr unary:
                        switch (unary.Op.Identifier)
                        {
                            case "+": result = unary.Argument; break;
                            case "-": result = -unary.Argument; break;
                        }
                        break;
                    case BinaryExpr binary:
                        switch (binary.Op.Identifier)
                        {
                            case "+": result = binary.Left + binary.Right; break;
                            case "-": result = binary.Left - binary.Right; break;
                            case "*": result = binary.Left * binary.Right; break;
                            case "/": result = binary.Left / binary.Right; break;
                        }
                        break;
                }

            } while (!result.Equals(prev));
            return result;
        }

        public Expr Substitute(params (Expr variable, Expr value)[] arguments)
        {
            Expr result = this;
            foreach (var (variable, value) in arguments)
            {
                result = result.Substitute(variable, value);
            }
            return result;
        }
        public Expr[] ToArray()
        {
            if (this.IsArray(out var array))
            {
                return array;
            }
            return new[] { this };
        }

        public Expr[][] ToMatrix()
        {
            if (IsMatrix(out var matrix))
            {
                return matrix;
            }
            return new[] { new[] { this } };
        }
        public static Expr Transpose(Expr right)
        {
            if (right.IsMatrix(out var matrix))
            {
                return Matrix(Transpose(matrix));
            }
            return right;
        }
        public static Expr[][] Transpose(Expr[][] matrix)
        {
            int n = matrix.Length;
            int m = n>0 ? matrix[0].Length : 0;
            var result = new Expr[m][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new Expr[n];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = matrix[j][i];
                }
                result[i] = row;
            }
            return result;
        }
        #endregion

        #region Operators
        public static Expr operator +(Expr a) => a;
        public static Expr operator -(Expr a) => Negate(a);
        public static Expr operator +(Expr a, Expr b) => Add(a, b);
        public static Expr operator -(Expr a, Expr b) => Subtract(a, b);
        public static Expr operator *(Expr a, Expr b) => Multiply(a, b);
        public static Expr operator /(Expr a, Expr b) => Divide(a, b);
        public static Expr operator ^(Expr a, Expr b) => Power(a, b);
        #endregion

        #region Vectorization
        public static bool IsVectorized(Expr a, Expr b, out Expr[] a_array, out Expr[] b_array)
        {
            if (a.IsArray(out a_array) | b.IsArray(out b_array))
            {
                if (b_array == null)
                {
                    b_array = new Expr[a_array.Length];
                    System.Array.Fill(b_array, b);
                }
                else if (a_array == null)
                {
                    a_array = new Expr[b_array.Length];
                    System.Array.Fill(a_array, a);
                }
                return true;
            }
            return false;
        }
        static Expr[] UnaryVectorOp(Expr[] a_array, Func<Expr, Expr> op)
        {
            Expr[] result = new Expr[a_array.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op(a_array[i]);
            }
            return result;
        }
        static Expr[] BinaryVectorOp(Expr[] a_array, Expr[] b_array, Func<Expr, Expr, Expr> op)
        {
            if (a_array.Length != b_array.Length)
            {
                throw new ArgumentException("Unequal lengths for vectors", nameof(b_array));
            }
            Expr[] result = new Expr[a_array.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op(a_array[i], b_array[i]);
            }
            return result;
        }
        #endregion

        #region Functions
        public static Expr Negate(Expr a)
        {
            if (a.IsArray(out Expr[] a_array))
            {
                return Array(UnaryVectorOp(a_array, Negate));
            }
            if (a.IsConstant(out double x))
            {
                return -x;
            }
            if (a.IsUnary("-", out Expr argA))
            {
                return argA;
            }
            return new UnaryExpr("-", a);
        }
        public static Expr Add(Expr a, Expr b)
        {
            if (IsVectorized(a, b, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(a_array, b_array, Add));
            }
            if (a.IsConstant(out double x) && b.IsConstant(out double y))
            {
                return x + y;
            }
            if (a.IsConstant(out double consA) && consA == 0)
            {
                return b;
            }
            if (b.IsConstant(out double consB) && consB == 0)
            {
                return a;
            }
            if (a.Equals(b))
            {
                return 2 * a;
            }
            if (a.IsUnary("-", out Expr argA)
                && b.IsUnary("-", out Expr argB))
            {
                return -(argB + argA);
            }
            if (b.IsUnary("-", out Expr argB2))
            {
                return a - argB2;
            }
            if (a.IsUnary("-", out Expr argA2))
            {
                return b - argA2;
            }
            if (a.IsFactor(out double a_factor, out var a_arg)
                && b.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_arg.Equals(b_arg))
                {
                    return (a_factor+b_factor)*a_arg;
                }
            }
            if (a.IsBinary("+", out var a_add_left, out var a_add_right))
            {
                if (b.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_add_left, a_add_right, b_add_left, b_add_right);
                }
                if (b.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_add_left, a_add_right, b_sub_left, -b_sub_right);
                }
            }
            if (a.IsBinary("-", out var a_sub_left, out var a_sub_right))
            {
                if (b.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_sub_left, -a_sub_right, b_add_left, b_add_right);
                }
                if (b.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_sub_left, -a_sub_right, b_sub_left, -b_sub_right);
                }
            }

            if (b.IsBinary("*", out var b_left, out var b_right))
            {
                if (b_left.IsConstant(out double b_left_val))
                {
                    if (b_left_val<0)
                    {
                        return Subtract(a, (-b_left_val)*b_right);
                    }
                }
            }

            return new BinaryExpr("+", a, b);
        }
        public static Expr Subtract(Expr a, Expr b)
        {
            if (IsVectorized(a, b, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(a_array, b_array, Subtract));
            }
            if (a.IsConstant(out double x) && b.IsConstant(out double y))
            {
                return x - y;
            }
            if (a.IsConstant(out double consA))
            {
                if (consA == 0)
                {
                    return -b;
                }
            }
            if (b.IsConstant(out double consB))
            {
                if (consB == 0)
                {
                    return a;
                }
            }
            if (a.Equals(b))
            {
                return 0;
            }
            if (a.IsUnary("-", out Expr argA)
                && b.IsUnary("-", out Expr argB))
            {
                return argB - argA;
            }
            if (b.IsUnary("-", out Expr argB2))
            {
                return a + argB2;
            }
            if (a.IsUnary("-", out Expr argA2))
            {
                return -(argA2 + b);
            }
            if (a.IsFactor(out double a_factor, out var a_arg)
                && b.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_arg.Equals(b_arg))
                {
                    return (a_factor-b_factor)*a_arg;
                }
            }
            if (a.IsBinary("+", out var a_add_left, out var a_add_right))
            {
                if (b.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_add_left, a_add_right, -b_add_left, -b_add_right);
                }
                if (b.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_add_left, a_add_right, -b_sub_left, b_sub_right);
                }
            }
            if (a.IsBinary("-", out var a_sub_left, out var a_sub_right))
            {
                if (b.IsBinary("+", out var b_add_left, out var b_add_right))
                {
                    return Sum(a_sub_left, -a_sub_right, -b_add_left, -b_add_right);
                }
                if (b.IsBinary("-", out var b_sub_left, out var b_sub_right))
                {
                    return Sum(a_sub_left, -a_sub_right, -b_sub_left, b_sub_right);
                }
            }

            if (b.IsBinary("*", out var b_left, out var b_right))
            {
                if (b_left.IsConstant(out double b_left_val))
                {
                    if (b_left_val<0)
                    {
                        return Add(a, (-b_left_val)*b_right);
                    }
                }
            }
            return new BinaryExpr("-", a, b);
        }
        public static Expr Multiply(Expr a, Expr b)
        {
            if (IsVectorized(a, b, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(a_array, b_array, Multiply));
            }
            if (a.IsUnary("sqrt", out Expr argA)
                && b.IsUnary("sqrt", out Expr argB))
            {
                return Sqrt(argA * argB);
            }
            if (a.IsConstant(out double x) && b.IsConstant(out double y))
            {
                return x * y;
            }
            if (a.IsConstant(out double a_const))
            {
                if (a_const == 0)
                {
                    return 0;
                }
                if (a_const == 1)
                {
                    return b;
                }
                if (b.IsBinary("*", out var b_mul_left, out var b_mul_right))
                {
                    if (b_mul_left.IsConstant(out double b_mul_left_const))
                    {
                        return (a_const * b_mul_left_const) * b_mul_right;
                    }
                    if (b_mul_right.IsConstant(out double b_mul_right_const))
                    {
                        return (a_const * b_mul_right_const) * b_mul_left;
                    }
                }
                if (b.IsBinary("/", out var b_div_left, out var b_div_right))
                {
                    if (b_div_left.IsConstant(out double b_div_left_const))
                    {
                        return (a_const * b_div_left_const) / b_div_right;
                    }
                    if (b_div_right.IsConstant(out double b_div_right_const))
                    {
                        return (a_const / b_div_right_const) * b_div_left;
                    }
                }
                if (Math.Abs(a_const)<0.2)
                {
                    Debug.WriteLine($"{a}*{b} = {b}*{1/a_const}");
                    return b/(1/a_const);
                }
            }
            if (b.IsConstant(out double b_const))
            {
                if (b_const == 0)
                {
                    return 0;
                }
                if (b_const == 1)
                {
                    return a;
                }

                if (Math.Abs(b_const)<1)
                {
                    return a / (1/b_const);
                }
                else
                {
                    return b_const * a;
                }
            }
            if (a.IsFactor(out double a_factor, out var a_arg)
                && b.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_factor!=1 || b_factor!=1)
                {
                    return (a_factor*b_factor)*(a_arg*b_arg);
                }
            }
            if (a.IsBinary(out var aop, out var a_left, out var a_right)
                && b.IsBinary(out var bop, out var b_left, out var b_right))
            {
                if (aop=="/" && bop=="/")
                {
                    // (x/a)*(y/b) = (x*y)/(a*b)
                    return (a_left*b_left)/(a_right*b_right);
                }
            }
            if (a.IsBinary(out aop, out a_left, out a_right))
            {
                if (aop=="*")
                {
                    if (a_left.IsConstant(out var a_left_const))
                    {
                        return a_left_const * (a_right  * b);
                    }
                    if (a_right.IsConstant(out var a_right_const))
                    {
                        return a_right_const * (a_left * b);
                    }
                }
                if (aop=="/")
                {
                    if (a_left.IsConstant(out var a_left_const))
                    {
                        return a_left_const*(b/a_right);
                    }
                    if (a_right.IsConstant(out var a_right_const))
                    {
                        return (a_left*b)/a_right_const;
                    }
                }
            }
            if (b.IsBinary(out bop, out b_left, out b_right))
            {
                if (bop=="*")
                {
                    if (b_left.IsConstant(out var b_left_const))
                    {
                        return b_left_const * (a*b_right);
                    }
                    if (b_right.IsConstant(out var b_right_const))
                    {
                        return b_right_const * (a*b_left);
                    }
                }
                if (bop=="/")
                {
                    if (b_left.IsConstant(out var b_left_const))
                    {
                        return b_left_const * (a/b_right);
                    }
                    if (b_right.IsConstant(out var b_right_const))
                    {
                        return (a*b_left)/b_right_const;
                    }
                }
            }
            if (a.Equals(b))
            {
                return a^2;
            }
            return new BinaryExpr("*", a, b);
        }

        public static Expr Divide(Expr a, Expr b)
        {
            if (IsVectorized(a, b, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(a_array, b_array, Divide));
            }
            if (a.IsConstant(out double x) && b.IsConstant(out double y))
            {
                return x / y;
            }
            if (a.IsConstant(out double a_const))
            {
                if (a_const == 0)
                {
                    return 0;
                }
                if (a_const<1)
                {
                    return 1/((1/a_const)*b);
                }
            }
            if (b.IsConstant(out double b_const))
            {
                if (b_const == 0)
                {
                    return double.PositiveInfinity;
                }
                if (b_const == 1)
                {
                    return a;
                }
                if (a.IsBinary("*", out var a_mul_left, out var a_mul_right))
                {
                    if (a_mul_left.IsConstant(out var a_mul_left_const))
                    {
                        return (a_mul_left_const/b_const) * a_mul_right;
                    }
                    if (a_mul_right.IsConstant(out var a_mul_right_const))
                    {
                        return (a_mul_right_const/b_const) * a_mul_left;
                    }
                }
                if (a.IsBinary("/", out var a_div_left, out var a_div_right))
                {
                    if (a_div_left.IsConstant(out var a_div_left_const))
                    {
                        return (a_div_left_const/b_const) / a_div_right;
                    }
                    if (a_div_right.IsConstant(out var a_div_right_const))
                    {
                        return a_div_left/(a_div_right_const*b_const);
                    }
                }
                if (Math.Abs(b_const)<0.2)
                {
                    Debug.WriteLine($"{a}/{b} = {1/b_const}*{a}");
                    return (1/b_const)*a;
                }
            }
            if (a.Equals(b))
            {
                return 1;
            }
            if (a.IsUnary("-", out Expr negA))
            {
                return Negate(negA / b);
            }
            if (b.IsUnary("-", out Expr negB))
            {
                return Negate(a / negB);
            }
            if (a.IsUnary("sqrt", out Expr argA) && argA == b)
            {
                return 1 / Sqr(b);
            }
            if (b.IsUnary("sqrt", out Expr argB) && argB == a)
            {
                return Sqr(a);
            }
            if (a.IsFactor(out double a_factor, out var a_arg)
                && b.IsFactor(out double b_factor, out var b_arg))
            {
                if (a_factor!=1 && b_factor!=1)
                {
                    return (a_factor/b_factor)*(a_arg/b_arg);
                }
            }
            if (a.IsBinary(out var aop, out var a_left, out var a_right)
                && b.IsBinary(out var bop, out var b_left, out var b_right))
            {
                if (aop=="/" && bop=="/")
                {
                    // (x/a)/(y/b) = (b*x)/(a*y)
                    return (a_left*b_right)/(a_right*b_left);
                }
            }
            if (b.IsBinary(out bop, out b_left, out b_right))
            {
                if (bop=="*")
                {
                    if (b_left.IsFactor(out var b_left_factor, out var b_left_arg) && b_left_factor!=1)
                    {
                        return (1/b_left_factor)*(a/(b_left_arg*b_right));
                    }
                    if (b_right.IsFactor(out var b_right_factor, out var b_right_arg) && b_right_factor!=1)
                    {
                        return (1/b_right_factor)*(a/(b_left  *b_right_arg));
                    }
                }
                if (bop=="/")
                {
                    if (b_left.IsFactor(out var b_left_factor, out var b_left_arg) && b_left_factor!=1)
                    {
                        return (1/b_left_factor)*(a*b_right/b_left_arg);
                    }
                    if (b_right.IsFactor(out var b_right_factor, out var b_right_arg) && b_right_factor!=1)
                    {
                        return (b_right_factor)*(a*b_right_arg/b_left);
                    }
                }
            }
            return new BinaryExpr("/", a, b);
        }
        public static Expr Power(Expr a, Expr b)
        {
            if (IsVectorized(a, b, out var a_array, out var b_array))
            {
                return Array(BinaryVectorOp(a_array, b_array, Power));
            }
            if (a.IsConstant(out double aConst))
            {
                if (b.IsConstant(out double bConst))
                {
                    return Math.Pow(aConst, bConst);
                }
                return Exp(Math.Log(aConst) * b);
            }
            return new BinaryExpr("^", a, b);
        }
        public static Expr Abs(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Abs));
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            if (a.IsUnary("-", out Expr a_arg))
            {
                return Abs(a_arg);
            }
            if (a.IsUnary("sqrt", out _))
            {
                return a;
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Sign(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Sign));
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Sqr(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Sqr));
            }
            if (a.IsConstant(out double x))
            {
                return x * x;
            }
            if (a.IsUnary("sqrt", out Expr a_arg))
            {
                return a_arg;
            }
            return a * a;
        }
        public static Expr Sqrt(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Sqrt));
            }
            if (a.IsBinary("*", out Expr argA, out Expr argB))
            {
                if (argA.Equals(argB))
                {
                    return Abs(argA);
                }
            }
            if (a.IsUnary("sqr", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Exp(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Exp));
            }
            if (a.IsUnary("log", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Log(Expr a, double newBase)
        {
            return Ln(a)/Math.Log(newBase);
        }
        public static Expr Ln(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Ln));
            }
            if (a.IsUnary("exp", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Sin(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Sin));
            }
            if (a.IsUnary("asin", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Cos(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Cos));
            }
            if (a.IsUnary("acos", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Tan(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Tan));
            }
            if (a.IsUnary("atan", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Sinh(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Sinh));
            }
            if (a.IsUnary("asinh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Cosh(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Cosh));
            }
            if (a.IsUnary("acosh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Tanh(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Tanh));
            }
            if (a.IsUnary("atanh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Asin(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Asin));
            }
            if (a.IsUnary("sin", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Acos(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Acos));
            }
            if (a.IsUnary("cos", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Atan(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Atan));
            }
            if (a.IsUnary("tan", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Asinh(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Asinh));
            }
            if (a.IsUnary("sinh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Acosh(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Acosh));
            }
            if (a.IsUnary("cosh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }
        public static Expr Atanh(Expr a)
        {
            if (a.IsArray(out var a_array))
            {
                return Array(UnaryVectorOp(a_array, Atanh));
            }
            if (a.IsUnary("tanh", out var a_arg))
            {
                return a_arg;
            }
            var op = UnaryOp.FromMethod();
            if (a.IsConstant(out double x))
            {
                return op.Function(x);
            }
            return new UnaryExpr(op, a);
        }

        #endregion

        #region Vector Functions
        public static Expr Cross(Expr A, Expr[] B)
        {
            if (B.Length==2)
            {
                return Array(-A*B[1], A*B[0]);
            }
            throw new NotSupportedException("Unknown cross product dimensions.");
        }
        public static Expr Cross(Expr[] A, Expr B)
        {
            if (A.Length==2)
            {
                return Array(A[1]*B, -A[0]*B);
            }
            throw new NotSupportedException("Unknown cross product dimensions.");
        }
        public static Expr Cross(Expr[] A, Expr[] B)
        {
            if (A.Length == 2 && B.Length == 2)
            {
                return A[0]*B[1] - A[1]*B[0];
            }
            else if (A.Length==1 && B.Length == 2)
            {
                return Cross(A[0], B);
            }
            else if (A.Length==2 && B.Length == 1)
            {
                return Cross(A, B[0]);
            }
            else if (A.Length==3 && B.Length==3)
            {
                return Array(
                    A[1]*B[2] - A[2]*B[1],
                    A[2]*B[0] - A[0]*B[2],
                    A[0]*B[1] - A[1]*B[0]);
            }
            throw new NotSupportedException("Unknown cross product dimensions.");
        }
        #endregion

        #region Parsing
        public static Expr Parse(string expression) => Parse(new Tokenizer(expression));
        static Expr Parse(Tokenizer tokenizer)
        {
            var parser = new ExprParser(tokenizer);
            return parser.ParseExpression();
        }
        #endregion

        #region Formatting
        public override string ToString()
            => ToString("g");
        public string ToString(string formatting)
            => ToString(formatting, null);
        public abstract string ToString(string formatting, IFormatProvider provider);
        #endregion                
    }

}
