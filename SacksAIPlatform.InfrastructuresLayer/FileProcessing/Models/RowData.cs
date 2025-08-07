namespace SacksAIPlatform.InfrastructuresLayer.FileProcessing
{
    using System.Collections.ObjectModel;

    public class RowData
    {
        public int Index { get; init; }

        public Collection<CellData> Cells { get; init; } = new Collection<CellData>();

        public RowData(int index)
        {
            Index = index;
        }


    }
}