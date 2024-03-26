using System;

namespace CloudX.Auto.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class TestCodeAttribute : Attribute
    {
        public string Code { get; }

        public TestCodeAttribute(string code)
        {
            Code = code;
        }
    }
}