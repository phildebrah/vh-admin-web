using System.Security.Claims;

namespace AdminWebsite.UnitTests.Security
{
    /// <summary>
    /// Mock class to ease testing of identity services
    /// </summary>
    public class TestPrincipal : ClaimsPrincipal 
    {
        public TestPrincipal(params Claim[] claims) : base(new TestIdentity(claims))
        {   
        }
    }
}