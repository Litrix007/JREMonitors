using JREMonitors.Core.Providers;
using JREMonitors.Core.Services;
using JREMonitors.JRE.Constants;

namespace JREMonitors.JRE.Services
{
    // ReSharper disable once InconsistentNaming
    public static class JREDelayServices
    {
        // ReSharper disable once InconsistentNaming
        public static DelayService CreateJREDelayService()
        {
            var delayService = new DelayService();
            delayService.Register(DelayTypes.Normal, new RandomDelayProvider(0.3f, 0.4f));
            delayService.Register(DelayTypes.Brake, new RandomDelayProvider(0.2f, 0.35f));
            delayService.Register(DelayTypes.Speed, new RandomDelayProvider(0.2f, 0.3f));
            delayService.Register(DelayTypes.Button, new RandomDelayProvider(0, 0));
            delayService.Register(DelayTypes.TIMSBlink, new RandomDelayProvider(0.4f, 0.4f));
            delayService.Register(DelayTypes.BcKpaBlink, new RandomDelayProvider(0.5f, 0.5f));
            return delayService;
        }
    }
}