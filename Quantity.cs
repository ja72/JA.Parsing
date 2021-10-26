using System;
using System.Linq;

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

    public record Scalar(double Value) : IQuantity
    {
        public static readonly Scalar Zero = 0;
        public static implicit operator Scalar(double value) => new(value);
        public static implicit operator double(Scalar scalar) => scalar.Value;
        public int Rank { get => 0; }
        double IQuantity.Value { get => Value; }
        double[] IQuantity.Array { get => new[] { Value }; }
        double[][] IQuantity.Array2 { get => new double[][] { new[] { Value } }; }

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider) => Value.ToString(formatting, formatProvider);
        #endregion
    }
    public record Vector(params double[] Elements) : IQuantity
    {
        public static readonly Vector Empty = Array.Empty<double>();
        public static implicit operator Vector(double[] array) => new (array);
        public static implicit operator double[](Vector vector) => vector.Elements;
        public int Rank { get => 1; }
        public int Size { get => Elements.Length; }
        double IQuantity.Value { get => 0; }
        double[] IQuantity.Array { get => Elements; }
        double[][] IQuantity.Array2 { get => new[] { Elements }; }
        public ref double this[int index] => ref Elements[index];

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider)
            => $"[{string.Join(",", Elements.Select((x) => x.ToString(formatting, formatProvider)))}]";
        #endregion
    }
    public record Matrix(double[][] Elements) : IQuantity
    {
        public static readonly Matrix Empty = Array.Empty<double[]>();
        public static implicit operator Matrix(double[][] matrix) => new(matrix);
        public static implicit operator double[][](Matrix matrix) => matrix.Elements;
        public Matrix(Vector[] matrix) :
            this(matrix.Select(row => row.Elements).ToArray())
        { }
        public int Rank { get => 2; }
        public int Rows { get => Elements.Length; }
        public int Columns { get => Elements.Length>0 ? Elements[0].Length : 0; }
        double IQuantity.Value { get => 0; }
        double[] IQuantity.Array { get => null; }
        double[][] IQuantity.Array2 { get => Elements; }
        public ref double this[int row, int column] => ref Elements[row][column];

        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider)
            => $"[{string.Join(",", Elements.Select(row => ((Vector)row).ToString(formatting, formatProvider)))}]";
        #endregion
    }

}
