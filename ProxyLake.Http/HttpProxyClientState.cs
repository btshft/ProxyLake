using ProxyLake.Http.ScheduledActivities;
using ProxyLake.Http.Utils;

namespace ProxyLake.Http
{
    internal class HttpProxyClientState
    {
        public CopyOnWriteCollection<HttpProxyHandlerState> HandlerStates { get; }
        public IScheduledActivity HealthCheckActivity { get; }
        
        public HttpProxyClientState(
            CopyOnWriteCollection<HttpProxyHandlerState> handlerStates, 
            IScheduledActivity healthCheckActivity)
        {
            HandlerStates = handlerStates;
            HealthCheckActivity = healthCheckActivity;
        }
    }
}