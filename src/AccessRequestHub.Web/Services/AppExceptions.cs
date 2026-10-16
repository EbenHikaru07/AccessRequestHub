namespace AccessRequestHub.Web.Services
{
    public class AppExceptions
    {
        public class RequestValidationException : Exception { public RequestValidationException(string m) : base(m) { } }
        public class ForbiddenException : Exception { public ForbiddenException(string m) : base(m) { } }
        public class NotFoundException : Exception { public NotFoundException(string m) : base(m) { } }
        public class ConflictException : Exception { public ConflictException(string m) : base(m) { } }
    }
}
