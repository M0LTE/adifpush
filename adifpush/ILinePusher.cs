using System.Threading.Tasks;

namespace adifpush
{
    interface ILinePusher
    {
        Task<PushLineResult[]> PushLines(string[] adifLines, bool showProgress);
        string InstanceUrl { get; }
    }
}