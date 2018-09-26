using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using SystemEx;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WampSharp.Binding;
using WampSharp.Tests.TestHelpers.Integration;
using WampSharp.V2;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Rpc;

namespace WampSharp.Tests.Wampv2.Client.Callee
{
    public class UnregisterCalleeTests
    {
        [Test]
        public void UnregisterOperation_NoMemoryLeaks()
        {
            var test = new UnregisterCalleeTest();
            test.Act();
            test.Assert();
        }
    }

    public class UnregisterCalleeTest : CalleeTest<JToken>
    {
        private readonly long mRegistrationId = 2147782813617642;
        private IAsyncDisposable mUnregisterCallback;

        public UnregisterCalleeTest()
            : base(new JTokenJsonBinding(), new JTokenEqualityComparer())
        {
        }

        public override void Act()
        {
            DealerMock dealer = new DealerMock();

            WampClientPlayground playground = new WampClientPlayground();

            dealer.SetRegisterCallback((callee, requestId) => callee.Registered(requestId, mRegistrationId));

            dealer.SetUnregisterCallback((callee, requestId) => callee.Unregistered(requestId));

            IWampChannel channel = playground.GetChannel(dealer, "realm1", mBinding);

            channel.Open();

            Task<IAsyncDisposable> register = channel.RealmProxy.RpcCatalog.Register(new OperationMock(), new RegisterOptions());

            mUnregisterCallback = register.Result;
                
            mUnregisterCallback.DisposeAsync().Wait();
        }

        public override void Assert()
        {
            object callee = 
                mUnregisterCallback.GetType().GetField("mCallee", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(mUnregisterCallback);
            var registrations =
                (ConcurrentDictionary<long, IWampRpcOperation>)callee.GetType().GetField("mRegistrations", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(callee);

            NUnit.Framework.Assert.False(registrations.ContainsKey(mRegistrationId));
        }
    }
}