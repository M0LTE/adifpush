using System.Threading.Tasks;

namespace adifpush
{
    interface ILinePusher
    {
        Task<PushLineResult[]> PushLines(string[] adifLines);
    }
}