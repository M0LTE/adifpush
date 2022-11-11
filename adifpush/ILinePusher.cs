using System;
using System.Threading.Tasks;

namespace adifpush
{
    interface ILinePusher
    {
        Task<PushLineResult[]> PushLines(string[] adifLines, bool showProgress, DateTime notBefore);
        string InstanceUrl { get; }
        string InstanceID { get; }
    }
}