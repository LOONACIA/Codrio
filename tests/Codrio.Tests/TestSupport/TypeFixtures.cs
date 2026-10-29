public class CodrioGlobalFixture
{
}

namespace Codrio.Tests.Fixtures
{
    public class Outer<T>
    {
        public class Inner<U>
        {
        }

        public class Leaf
        {
        }
    }

#pragma warning disable CS8981
    public class @class
    {
    }
#pragma warning restore CS8981
}

namespace Codrio.Tests.Fixtures.@event
{
    public class Handler
    {
    }
}

namespace Codrio.Tests
{
    internal static class TypeFixtures
    {
        internal const string Source = """
        public class CodrioGlobalFixture
        {
        }

        namespace Codrio.Tests.Fixtures
        {
            public class Outer<T>
            {
                public class Inner<U>
                {
                }

                public class Leaf
                {
                }
            }

        #pragma warning disable CS8981
            public class @class
            {
            }
        #pragma warning restore CS8981
        }

        namespace Codrio.Tests.Fixtures.@event
        {
            public class Handler
            {
            }
        }
        """;
    }
}
