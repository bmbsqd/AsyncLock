using System.Security.Principal;

namespace Bmbsqd.Async.Tests
{
	public class FakePrincipal : IPrincipal, IIdentity
	{
		public FakePrincipal( string name )
		{
			Name = name;
		}

		public bool IsInRole( string role )
		{
			return false;
		}

		public IIdentity Identity { get { return this; } }
		public string Name { get; private set; }
		public string AuthenticationType { get { return "Fake"; } }
		public bool IsAuthenticated { get { return true; } }

		public override string ToString()
		{
			return Name;
		}
	}
}