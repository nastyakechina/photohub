namespace PhotoHub.Shared.Errors;

public static class CommonErrors
{
    public static readonly Error NotFound = new("Common.NotFound", "Resource was not found.");

    public static readonly Error Validation = new("Common.Validation", "Validation failed.");

    public static readonly Error Unauthorized = new("Common.Unauthorized", "User is not authorized.");

    public static readonly Error Conflict = new("Common.Conflict", "Resource conflict occurred.");

    public static readonly Error Internal = new("Common.Internal", "Internal server error occurred.");
}
