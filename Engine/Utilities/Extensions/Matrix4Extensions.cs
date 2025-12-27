using OpenTK.Mathematics;

namespace Engine.Utilities;

/// <summary>
/// Data transfer object for serializing Matrix4 data.
/// Stores the 16 matrix elements in row-major order.
/// </summary>
internal class MatrixSaveData
{
    #region Constants

    private const int MATRIX_ELEMENT_COUNT = 16;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the 16 elements of the matrix in row-major order.
    /// </summary>
    public float[] Elements { get; set; } = new float[MATRIX_ELEMENT_COUNT];

    #endregion
}

/// <summary>
/// Extension methods for Matrix4 to support serialization and deserialization.
/// </summary>
internal static class Matrix4Extensions
{
    #region Constants

    private const int MATRIX_ELEMENT_COUNT = 16;

    #endregion

    #region Serialization

    /// <summary>
    /// Converts a Matrix4 to a serializable save data object.
    /// </summary>
    /// <param name="matrix">The matrix to convert.</param>
    /// <returns>A MatrixSaveData object containing the matrix elements.</returns>
    public static MatrixSaveData GetSaveData(this Matrix4 matrix)
    {
        return new MatrixSaveData
        {
            Elements = ExtractMatrixElements(matrix)
        };
    }

    private static float[] ExtractMatrixElements(Matrix4 matrix)
    {
        return
        [
            // Row 1
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            // Row 2
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            // Row 3
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            // Row 4
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        ];
    }

    #endregion

    #region Deserialization

    /// <summary>
    /// Converts an array of 16 float values to a Matrix4.
    /// </summary>
    /// <param name="elements">The array containing 16 matrix elements in row-major order.</param>
    /// <returns>A Matrix4 constructed from the elements.</returns>
    /// <exception cref="ArgumentException">Thrown when the array does not contain exactly 16 elements.</exception>
    public static Matrix4 ToMatrix4(this float[] elements)
    {
        ValidateElementCount(elements);
        return ConstructMatrix4(elements);
    }

    private static void ValidateElementCount(float[] elements)
    {
        if (elements == null)
            throw new ArgumentNullException(
                nameof(elements),
                "Matrix element array cannot be null."
            );

        if (elements.Length != MATRIX_ELEMENT_COUNT)
            throw new ArgumentException(
                $"Matrix element array must contain exactly {MATRIX_ELEMENT_COUNT} elements, but received {elements.Length}.",
                nameof(elements)
            );
    }

    private static Matrix4 ConstructMatrix4(float[] elements)
    {
        return new Matrix4(
            // Row 1
            elements[0], elements[1], elements[2], elements[3],
            // Row 2
            elements[4], elements[5], elements[6], elements[7],
            // Row 3
            elements[8], elements[9], elements[10], elements[11],
            // Row 4
            elements[12], elements[13], elements[14], elements[15]
        );
    }

    #endregion
}