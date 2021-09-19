using System;

namespace MindustryCompiler.Code
{
    public class InvalidPreprocessorStatementException : Exception
    {
        public InvalidPreprocessorStatementException() : base() { }
        public InvalidPreprocessorStatementException(string msg) : base(msg) { }
    }

    public class InvalidCodeStatementException : Exception
    {
        public InvalidCodeStatementException() : base() { }
        public InvalidCodeStatementException(string msg) : base(msg) { }
    }

    public class InvalidArgumentStatementException : Exception
    {
        public InvalidArgumentStatementException() : base() { }
        public InvalidArgumentStatementException(string msg) : base(msg) { }
    }

    public class InvalidFunctionDefinitionException : Exception
    {
        public InvalidFunctionDefinitionException() : base() { }
        public InvalidFunctionDefinitionException(string msg) : base(msg) { }
    }

    public class InvalidVariableNameException : Exception
    {
        public InvalidVariableNameException() : base() { }
        public InvalidVariableNameException(string msg) : base(msg) { }
    }

    public class InvalidFunctionNameException : Exception
    {
        public InvalidFunctionNameException() : base() { }
        public InvalidFunctionNameException(string msg) : base(msg) { }
    }

    public class InvalidLabelNameException : Exception
    {
        public InvalidLabelNameException() : base() { }
        public InvalidLabelNameException(string msg) : base(msg) { }
    }

    public class InvalidOperatorException : Exception
    {
        public InvalidOperatorException() : base() { }
        public InvalidOperatorException(string msg) : base(msg) { }
    }

    public class DeviceAlreadyDefinedException : Exception
    {
        public DeviceAlreadyDefinedException() : base() { }
        public DeviceAlreadyDefinedException(string msg) : base(msg) { }
    }

    public class MissingDeviceException : Exception
    {
        public MissingDeviceException() : base() { }
        public MissingDeviceException(string msg) : base(msg) { }
    }
}
