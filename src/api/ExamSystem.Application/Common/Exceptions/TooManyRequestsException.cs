namespace ExamSystem.Application.Common.Exceptions;

public class TooManyRequestsException(string message) : InvalidOperationException(message);