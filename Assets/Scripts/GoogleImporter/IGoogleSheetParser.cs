using Cysharp.Threading.Tasks;

namespace GoogleImporter
{
    public interface IGoogleSheetParser
    {
        public UniTask Parse(string header, string token);
    }
}