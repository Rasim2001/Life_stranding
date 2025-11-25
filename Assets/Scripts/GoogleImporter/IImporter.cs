using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleImporter
{
    public interface IImporter
    {
        Task DownloadAndParseSheet(string sheetName, IGoogleSheetParser googleSheetParser);
        Task AppendRow(string sheetName, IList<object> row);
        Task UpdateRange(string sheetName, string range, IList<IList<object>> values);
    }
}