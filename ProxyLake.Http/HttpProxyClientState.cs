using ProxyLake.Http.ScheduledActivities;
using ProxyLake.Http.Utils;

namespace ProxyLake.Http
{
    internal class HttpProxyClientState
    {
        public CopyOnWriteCollection<HttpProxyHandlerState> HandlerStates { get; }
        public IScheduledActivity HealthCheckActivity { get; }
        
        public IScheduledActivity RefillActivity { get; }
        
        public HttpProxyClientState(
            CopyOnWriteCollection<HttpProxyHandlerState> handlerStates, 
            IScheduledActivity healthCheckActivity, 
            IScheduledActivity refillActivity)
        {
            HandlerStates = handlerStates;
            HealthCheckActivity = healthCheckActivity;
            RefillActivity = refillActivity;
        }
    }
}