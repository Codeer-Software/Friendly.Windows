using System;

namespace Codeer.Friendly.Windows.Step
{
    public class AppDomainBridge
    {
        public int GetCurrentDomainId(string arg)
        {
            return AppDomain.CurrentDomain.Id;
        }
    }
}
