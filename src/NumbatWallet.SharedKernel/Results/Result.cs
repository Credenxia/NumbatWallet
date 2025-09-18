namespace NumbatWallet.SharedKernel.Results;

public class Result
{
    protected Result(bool isSuccess, DomainError? error)
    {
        if (isSuccess && error != DomainError.None)
        {
            throw new InvalidOperationException("Cannot create a successful result with an error.");
        }

        if (!isSuccess && error == DomainError.None)
        {
            throw new InvalidOperationException("Cannot create a failed result without an error.");
        }

        IsSuccess = isSuccess;
        Error = error ?? DomainError.None;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public DomainError Error { get; }

    public static Result Success() => new(true, DomainError.None);
    public static Result Failure(DomainError error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, DomainError.None);
    public static Result<T> Failure<T>(DomainError error) => new(default, false, error);

    public static implicit operator Result(DomainError error) => Failure(error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, DomainError error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(DomainError error) => Failure<T>(error);
}