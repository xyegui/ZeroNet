using System;
using Agebull.Common.Rpc;
using Agebull.ZeroNet.Core;

namespace Agebull.ZeroNet.ZeroApi
{
    /// <summary>
    /// 性能计数器
    /// </summary>
    internal sealed class ApiClientCountHandler : ApiClient.IHandler
    {
        private CountData count;
        void ApiClient.IHandler.Prepare(ApiClient item)
        {
            count = new CountData
            {
                Start = DateTime.Now.Ticks,
                FromId = GlobalContext.RequestInfo.CallGlobalId,
                Requester = ZeroApplication.Config.RealName,
                HostName = item.Station,
                ApiName = item.Commmand
            };
        }

        void ApiClient.IHandler.End(ApiClient item)
        {
            count.ToId = GlobalContext.RequestInfo.LocalGlobalId;
            count.End = DateTime.Now.Ticks;
            count.Status = item.State.ToOperatorStatus(true);
            ApiCounter.Instance.Count(count);
        }
    }
}