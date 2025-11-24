namespace IServ.Exceptions;

/// <summary>
/// Exception thrown when IServ API operations fail.
/// </summary>
public class IServException : Exception
{
    /// <summary>
    /// Initializes a new instance of the IServException class.
    /// </summary>
    public IServException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the IServException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IServException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the IServException class with a specified error message
    /// and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public IServException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
