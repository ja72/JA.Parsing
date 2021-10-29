using System;
using JA.Expressions;

namespace JA
{
    /// <summary>
    /// Represents the return type of an expression evaluation, <see cref="Expressions.Expr.Eval((string sym, double val)[])"/>. 
    /// It might be a scalar, a vector or a matrix.
    /// </summary>    
    public interface IQuantity : IFormattable
    {
        public double Value { get; }
        public double[] Array { get; }
        public double[][] Array2 { get; }
        public bool IsScalar { get => Rank==0; }
        public bool IsArray { get => Rank==1; }
        public bool IsMatrix { get => Rank==2; }
        public int Rank { get; }
        public static IQuantity Scalar(double value) => new Scalar(value);
        public static IQuantity Vector(params double[] elements) => new Vector(elements);
        public static IQuantity Matrix(double[][] elements) => new Matrix(elements);
        public string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);        
    }

}
